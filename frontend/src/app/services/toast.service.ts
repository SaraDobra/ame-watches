import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface Toast {
  id: number;
  message: string;
  linkLabel?: string;
  linkUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private nextId = 1;

  readonly toasts = signal<Toast[]>([]);

  show(message: string, link?: { label: string; url: string }): void {
    const toast: Toast = { id: this.nextId++, message, linkLabel: link?.label, linkUrl: link?.url };
    this.toasts.update(list => [...list.slice(-2), toast]);

    if (this.isBrowser) {
      setTimeout(() => this.dismiss(toast.id), 4500);
    }
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(toast => toast.id !== id));
  }
}
