import { Directive, ElementRef, afterNextRender, inject, input } from '@angular/core';

@Directive({ selector: '[appReveal]' })
export class RevealDirective {
  readonly delay = input<number | string>(0, { alias: 'appReveal' });
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  constructor() {
    afterNextRender(() => {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (reduced || !('IntersectionObserver' in window)) return;

      const top = this.element.getBoundingClientRect().top;
      if (top < window.innerHeight * 0.92) return;

      this.element.classList.add('rv');
      this.element.style.transitionDelay = `${Number(this.delay()) || 0}ms`;

      const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
          this.element.classList.add('rv-in');
          observer.disconnect();
        }
      }, { threshold: 0.12, rootMargin: '0px 0px -6% 0px' });
      observer.observe(this.element);
    });
  }
}
