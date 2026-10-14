import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GALLERY_LAYOUT_OPTIONS, GalleryLayoutOption } from '../../../models/about.model';

@Component({
  selector: 'app-gallery-layout-selector',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gallery-layout-selector.html',
  styleUrl: './gallery-layout-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GalleryLayoutSelectorComponent {
  @Input() selectedLayout: string = 'grid-3';
  @Input() imageCount: number = 0;
  @Output() layoutChange = new EventEmitter<string>();

  readonly layoutOptions: GalleryLayoutOption[] = GALLERY_LAYOUT_OPTIONS;

  selectLayout(layoutId: string): void {
    if (this.selectedLayout !== layoutId) {
      this.selectedLayout = layoutId;
      this.layoutChange.emit(layoutId);
    }
  }
}
