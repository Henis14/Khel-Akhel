import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** /track-order — online orders are not live yet, so this page says so and points to the studio. */
@Component({
  selector: 'app-track-order',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './track-order.html',
  styleUrl: './track-order.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrackOrder {}
