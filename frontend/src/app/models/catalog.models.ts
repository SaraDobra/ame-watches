export interface Brand {
  id: number;
  name: string;
  slug: string;
  productCount: number;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  brandSlug: string;
  productCount: number;
}

export interface ProductListItem {
  id: number;
  name: string;
  slug: string;
  modelCode: string;
  brand: string;
  brandSlug: string;
  price: number;
  inStock: boolean;
  gender: string;
  type: string;
  imageUrl: string | null;
  secondImageUrl: string | null;
  category?: string | null;
  categorySlug?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ProductFilter {
  brand?: string;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  gender?: string;
  type?: string;
  search?: string;
  sort?: 'Newest' | 'PriceAsc' | 'PriceDesc' | 'Name';
  page?: number;
  pageSize?: number;
}

export interface ProductDetail {
  id: number;
  name: string;
  slug: string;
  modelCode: string;
  brand: string;
  brandSlug: string;
  description: string | null;
  price: number;
  stock: number;
  inStock: boolean;
  gender: string;
  type: string;
  caseSizeMm: number | null;
  waterResistance: string | null;
  movement: string | null;
  images: string[];
  category?: string | null;
  categorySlug?: string | null;
}

export interface PriceRange {
  min: number;
  max: number;
}

export interface ProductSuggestion {
  name: string;
  brand: string;
  modelCode: string;
  slug: string;
  price: number;
  imageUrl: string | null;
}
