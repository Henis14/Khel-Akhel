import { ChangeDetectionStrategy, Component, input } from '@angular/core';


@Component({
  selector: 'app-calligraphy-title',
  standalone: true,
  templateUrl: './calligraphy-title.html',
  styleUrl: './calligraphy-title.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalligraphyTitleComponent {
  /** The script word, e.g. 'सवाल' */
  readonly word = input<string>('');
  /** BCP-47 language of the word (hi, pa, ur) */
  readonly lang = input<string>('hi');
  /** left | center | auto (left on wide screens, centred on phones) */
  readonly align = input<'left' | 'center' | 'auto'>('left');
  /** sm for cards, md for page headers, lg for heroes */
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  /** Optional artwork from Daras that replaces the font-rendered word */
  readonly image = input<string | null>(null);
}
