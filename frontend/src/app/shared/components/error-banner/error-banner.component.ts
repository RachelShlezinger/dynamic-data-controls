import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  afterNextRender,
  input,
  output,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-error-banner',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './error-banner.component.html',
  styleUrl: './error-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorBannerComponent {
  readonly message = input.required<string>();
  readonly closed = output<void>();

  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');

  constructor() {
    // Move focus to the close button as soon as the banner renders so screen
    // readers announce the error immediately (requirement 9.7).
    afterNextRender(() => {
      this.closeButton()?.nativeElement.focus();
    });
  }

  /** Dismiss banner via Escape key — fires for any focus within the component. */
  @HostListener('keydown.escape')
  close(): void {
    this.closed.emit();
  }
}
