import {Directive, ElementRef, HostListener, Input, OnDestroy, Renderer2} from '@angular/core';

/**
 * Lightweight CSS tooltip. Reuses the `ngbTooltip` attribute name so existing templates
 * keep working after dropping the @ng-bootstrap/ng-bootstrap dependency (whose only used
 * features were the tooltip and a confirm modal).
 */
@Directive({
  selector: '[ngbTooltip]',
  standalone: false,
})
export class TooltipDirective implements OnDestroy {
  @Input('ngbTooltip') text: string | null = '';

  private tooltipEl: HTMLElement | null = null;

  constructor(private host: ElementRef, private renderer: Renderer2) {}

  @HostListener('mouseenter')
  @HostListener('focus')
  show(): void {
    if (this.tooltipEl != null || !this.text) {
      return;
    }

    const el = this.renderer.createElement('div');
    this.renderer.addClass(el, 'app-tooltip');
    this.renderer.appendChild(el, this.renderer.createText(this.text));
    this.renderer.appendChild(document.body, el);
    this.tooltipEl = el;

    const rect = (this.host.nativeElement as HTMLElement).getBoundingClientRect();
    this.renderer.setStyle(el, 'position', 'fixed');
    this.renderer.setStyle(el, 'top', `${rect.bottom + 6}px`);
    this.renderer.setStyle(el, 'left', `${rect.left + rect.width / 2}px`);
    this.renderer.setStyle(el, 'transform', 'translateX(-50%)');
    this.renderer.setStyle(el, 'z-index', '2000');
  }

  @HostListener('mouseleave')
  @HostListener('blur')
  hide(): void {
    if (this.tooltipEl != null) {
      this.renderer.removeChild(document.body, this.tooltipEl);
      this.tooltipEl = null;
    }
  }

  ngOnDestroy(): void {
    this.hide();
  }
}
