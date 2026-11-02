import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';

@Directive({ selector: 'img[appFade]' })
export class FadeImageDirective {
  private readonly image = inject<ElementRef<HTMLImageElement>>(ElementRef).nativeElement;

  constructor() {
    afterNextRender(() => {
      if (this.image.complete) return;

      this.image.classList.add('img-loading');
      const done = () => this.image.classList.remove('img-loading');
      this.image.addEventListener('load', done, { once: true });
      this.image.addEventListener('error', done, { once: true });
    });
  }
}
