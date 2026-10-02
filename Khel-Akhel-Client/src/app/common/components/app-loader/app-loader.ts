import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-loader.html',
  styleUrl: './app-loader.scss'
})
export class AppLoaderComponent {
  @Input() size: 'small' | 'medium' = 'medium';
  @Input() text?: string;
}
