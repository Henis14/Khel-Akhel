import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CalligraphyTitleComponent } from '../calligraphy-title/calligraphy-title';

export type EmptyStateArt = 'bag' | 'heart' | 'kite' | 'pot';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [RouterLink, CalligraphyTitleComponent],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly art = input<EmptyStateArt>('bag');
  /** Optional artwork from Daras that replaces the built-in drawing */
  readonly image = input<string | null>(null);

  readonly word = input<string>('');
  readonly wordLang = input<string>('hi');
  readonly title = input.required<string>();
  readonly message = input<string>('');
  /** h1 on full pages (404, cart), h2 when the state sits inside a page */
  readonly headingLevel = input<1 | 2>(1);

  readonly primaryLabel = input<string>('');
  readonly primaryLink = input<string>('/');
  readonly secondaryLabel = input<string>('');
  /** When set the secondary action is a link; otherwise it emits secondaryClick */
  readonly secondaryLink = input<string | null>(null);

  readonly secondaryClick = output<void>();
}
