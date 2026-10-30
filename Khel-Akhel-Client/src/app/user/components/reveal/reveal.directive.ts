import { AfterViewInit, Directive, ElementRef, Input, OnDestroy, inject } from '@angular/core';

/**
 * Adds `is-revealed` to an element the first time it scrolls into view.
 * Pair with the `.ka-reveal` (fade up) or `.ka-reveal-img` (unveil) classes
 * from user-theme.scss. Purely visual: content is in the DOM from the start,
 * and everything shows at once when reduced motion is preferred or
 * IntersectionObserver is unavailable.
 */
@Directive({
  selector: '[kaReveal]',
  standalone: true,
})
export class RevealDirective implements AfterViewInit, OnDestroy {
  /** Optional delay in ms, for staggering siblings */
  @Input() kaReveal: number | string | '' = '';

  private el = inject(ElementRef<HTMLElement>);
  private observer?: IntersectionObserver;

  ngAfterViewInit(): void {
    const node = this.el.nativeElement as HTMLElement;
    const delay = Number(this.kaReveal) || 0;
    if (delay) node.style.transitionDelay = `${delay}ms`;

    const reduce =
      typeof window === 'undefined' ||
      !('IntersectionObserver' in window) ||
      window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduce) {
      node.classList.add('is-revealed');
      return;
    }

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            node.classList.add('is-revealed');
            this.observer?.disconnect();
          }
        }
      },
      { rootMargin: '0px 0px -8% 0px', threshold: 0.08 },
    );
    this.observer.observe(node);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
