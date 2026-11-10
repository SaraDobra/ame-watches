import { Component, DestroyRef, afterNextRender, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminApiService } from '../admin-api.service';
import { AdminCategory, AdminProductDetail, ProductInput } from '../admin.models';
import { CatalogService } from '../../services/catalog.service';
import { Brand } from '../../models/catalog.models';
import { ImgPipe } from '../../pipes/img.pipe';

const TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/avif'];
const MAX_BYTES = 5 * 1024 * 1024;
const MAX_IMAGES = 8;

@Component({
  selector: 'app-admin-product-form',
  imports: [ReactiveFormsModule, RouterLink, ImgPipe],
  templateUrl: './admin-product-form.component.html',
  styleUrl: './admin-product-form.component.scss'
})
export class AdminProductFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AdminApiService);
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly maxImages = MAX_IMAGES;
  readonly id = Number(this.route.snapshot.paramMap.get('id')) || null;

  product = signal<AdminProductDetail | null>(null);
  brands = signal<Brand[]>([]);
  categories = signal<AdminCategory[]>([]);
  loading = signal(this.id !== null);
  saving = signal(false);
  errors = signal<string[]>([]);
  saved = signal('');
  createdNotice = signal(this.route.snapshot.queryParamMap.has('created'));
  uploading = signal(false);
  imageError = signal('');
  dragging = signal(false);
  deleting = signal<number | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    modelCode: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    brandId: [0, [Validators.min(1)]],
    categoryId: [null as number | null],
    price: [0, [Validators.required, Validators.min(0.01), Validators.max(100000)]],
    stock: [0, [Validators.required, Validators.min(0), Validators.max(100000)]],
    gender: ['Women'],
    type: ['Analog'],
    caseSizeMm: [null as number | null, [Validators.min(10), Validators.max(100)]],
    waterResistance: ['', Validators.maxLength(50)],
    movement: ['', Validators.maxLength(50)],
    description: ['', Validators.maxLength(2000)],
    isActive: [true]
  });

  constructor() {
    afterNextRender(() => {
      this.api.categories().subscribe({ next: list => this.categories.set(list), error: () => this.categories.set([]) });

      this.form.controls.brandId.valueChanges.subscribe(() => {
        const current = this.form.controls.categoryId.value;
        if (current !== null && !this.categoryOptions().some(c => c.id === Number(current))) {
          this.form.controls.categoryId.setValue(null);
        }
      });

      this.catalog.getBrands().subscribe(brands => {
        this.brands.set(brands);
        if (this.id === null && brands.length > 0 && this.form.controls.brandId.value === 0) {
          this.form.controls.brandId.setValue(brands[0].id);
        }
      });

      if (this.id !== null) {
        this.api.product(this.id).subscribe({
          next: product => { this.fill(product); this.loading.set(false); },
          error: () => { this.loading.set(false); }
        });
      }
    });
  }

  categoryOptions(): AdminCategory[] {
    const brandId = Number(this.form.controls.brandId.value);
    return this.categories().filter(c => c.brandId === brandId);
  }

  bad(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  save(): void {
    this.errors.set([]);
    this.saved.set('');
    this.createdNotice.set(false);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errors.set(['Plotësoni saktë fushat e shënuara.']);
      return;
    }

    const value = this.form.getRawValue();
    const input: ProductInput = {
      ...value,
      brandId: Number(value.brandId),
      categoryId: value.categoryId === null || (value.categoryId as unknown) === '' ? null : Number(value.categoryId),
      price: Number(value.price),
      stock: Number(value.stock),
      caseSizeMm: value.caseSizeMm === null || (value.caseSizeMm as unknown) === '' ? null : Number(value.caseSizeMm),
      waterResistance: value.waterResistance.trim() || null,
      movement: value.movement.trim() || null,
      description: value.description.trim() || null
    };

    this.saving.set(true);
    const request = this.id === null ? this.api.createProduct(input) : this.api.updateProduct(this.id, input);
    request.subscribe({
      next: product => {
        this.saving.set(false);
        if (this.id === null) {
          this.router.navigate(['/admin/produktet', product.id], { queryParams: { created: 1 } });
        } else {
          this.fill(product);
          this.saved.set('Ndryshimet u ruajtën.');
        }
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.errors.set(this.messages(err));
      }
    });
  }

  onPick(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.upload(Array.from(input.files ?? []));
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  upload(files: File[]): void {
    const product = this.product();
    if (!product || files.length === 0) return;

    this.imageError.set('');
    const bad = files.find(file => !TYPES.includes(file.type) || file.size > MAX_BYTES);
    if (bad) {
      this.imageError.set(`"${bad.name}" duhet të jetë JPG, PNG, WebP ose AVIF deri në 5 MB.`);
      return;
    }
    if (product.images.length + files.length > MAX_IMAGES) {
      this.imageError.set(`Një orë mund të ketë deri në ${MAX_IMAGES} foto. Mund të shtoni edhe ${MAX_IMAGES - product.images.length}.`);
      return;
    }

    this.uploading.set(true);
    this.api.uploadImages(product.id, files).subscribe({
      next: updated => { this.product.set(updated); this.uploading.set(false); },
      error: (err: unknown) => { this.imageError.set(this.messages(err).join(' ')); this.uploading.set(false); }
    });
  }

  move(index: number, delta: number): void {
    const product = this.product();
    if (!product) return;

    const ids = product.images.map(image => image.id);
    const target = index + delta;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    this.reorder(ids);
  }

  makePrimary(index: number): void {
    const product = this.product();
    if (!product || index === 0) return;

    const ids = product.images.map(image => image.id);
    const [picked] = ids.splice(index, 1);
    this.reorder([picked, ...ids]);
  }

  remove(imageId: number): void {
    const product = this.product();
    if (!product) return;

    this.imageError.set('');
    this.api.deleteImage(product.id, imageId).subscribe({
      next: updated => { this.product.set(updated); this.deleting.set(null); },
      error: (err: unknown) => { this.imageError.set(this.messages(err).join(' ')); this.deleting.set(null); }
    });
  }

  private reorder(ids: number[]): void {
    const product = this.product();
    if (!product) return;

    this.api.reorderImages(product.id, ids).subscribe({
      next: updated => this.product.set(updated),
      error: (err: unknown) => this.imageError.set(this.messages(err).join(' '))
    });
  }

  private fill(product: AdminProductDetail): void {
    this.product.set(product);
    this.form.reset({
      name: product.name,
      modelCode: product.modelCode,
      brandId: product.brandId,
      categoryId: product.categoryId ?? null,
      price: product.price,
      stock: product.stock,
      gender: product.gender,
      type: product.type,
      caseSizeMm: product.caseSizeMm,
      waterResistance: product.waterResistance ?? '',
      movement: product.movement ?? '',
      description: product.description ?? '',
      isActive: product.isActive
    });
  }

  private messages(err: unknown): string[] {
    if (!(err instanceof HttpErrorResponse)) return ['Ndodhi një gabim i papritur.'];
    if (err.status === 0) return ['Serveri nuk përgjigjet. A është ndezur API-ja?'];
    if (typeof err.error?.message === 'string') return [err.error.message];
    if (err.error?.errors && typeof err.error.errors === 'object') {
      return Object.values(err.error.errors).flatMap(list => list as string[]);
    }
    return [`Gabim ${err.status}. Provoni përsëri.`];
  }
}
