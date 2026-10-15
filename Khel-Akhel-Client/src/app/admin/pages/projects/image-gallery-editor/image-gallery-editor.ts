import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectImage, ProjectImageOrderDto } from '../../../models/project.model';
import { AdminProjectService } from '../../../services/admin-project.service';

export interface EditorImageItem {
  id: string;
  isExisting: boolean;
  encryptedImageId?: string;
  file?: File;
  previewUrl: string;
  fileName: string;
  fileSize: number;
  displayOrder: number;
}

@Component({
  selector: 'app-image-gallery-editor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './image-gallery-editor.html',
  styleUrl: './image-gallery-editor.scss',
})
export class ImageGalleryEditorComponent implements OnInit, OnChanges, OnDestroy {
  private projectService = inject(AdminProjectService);

  @Input() existingImages: ProjectImage[] = [];
  @Output() imagesChanged = new EventEmitter<{
    allImages: EditorImageItem[];
    newFiles: File[];
    removeImageIds: string[];
    imageOrders: ProjectImageOrderDto[];
  }>();

  readonly maxImages = 15;
  readonly maxFileSize = 5 * 1024 * 1024; // 5 MB

  items: EditorImageItem[] = [];
  removedImageIds: string[] = [];

  isDragOver = false;
  draggedIndex: number | null = null;
  dragOverIndex: number | null = null;
  errorMessage: string | null = null;
  private isDestroyed = false;

  ngOnInit(): void {
    this.syncExistingImages();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['existingImages'] && !changes['existingImages'].firstChange) {
      this.syncExistingImages();
    }
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    for (const item of this.items) {
      if (item.previewUrl && item.previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(item.previewUrl);
      }
    }
  }

  private syncExistingImages(): void {
    if (!this.existingImages) return;

    // Preserve any existing newFiles if already picked
    const newItems = this.items.filter((it) => !it.isExisting);

    const existingMapped: EditorImageItem[] = this.existingImages.map((img, idx) => ({
      id: img.encryptedImageId || `existing-${idx}`,
      isExisting: true,
      encryptedImageId: img.encryptedImageId,
      previewUrl: this.projectService.getImageUrl(img.imageUrl || img.imagePath),
      fileName: img.originalFileName || `Image ${idx + 1}`,
      fileSize: img.fileSize || 0,
      displayOrder: img.displayOrder || idx + 1,
    }));

    // Filter out already removed
    const activeExisting = existingMapped.filter(
      (m) => !this.removedImageIds.includes(m.encryptedImageId || ''),
    );

    this.items = [...activeExisting, ...newItems];
    this.recalculateOrders();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.processFiles(Array.from(input.files));
      input.value = ''; // Reset input to allow selecting same file again if desired
    }
  }

  onDragEnter(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.processFiles(Array.from(event.dataTransfer.files));
    }
  }

  private processFiles(files: File[]): void {
    this.errorMessage = null;
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'image/svg+xml'];

    const currentTotal = this.items.length;
    const remainingSlots = this.maxImages - currentTotal;

    if (remainingSlots <= 0) {
      this.errorMessage = `Maximum limit of ${this.maxImages} images already reached for this section.`;
      return;
    }

    if (files.length > remainingSlots) {
      this.errorMessage = `You can only add ${remainingSlots} more image(s) to stay within the ${this.maxImages}-image limit.`;
    }

    const filesToAdd = files.slice(0, remainingSlots);

    for (const file of filesToAdd) {
      // Validate type
      if (!allowedTypes.includes(file.type.toLowerCase())) {
        this.errorMessage = `File "${file.name}" is not a supported image format. Allowed formats: JPEG, PNG, WEBP, GIF.`;
        continue;
      }

      // Validate size
      if (file.size > this.maxFileSize) {
        this.errorMessage = `File "${file.name}" exceeds the maximum allowed size of 5 MB (${(file.size / (1024 * 1024)).toFixed(2)} MB).`;
        continue;
      }

      const previewUrl = URL.createObjectURL(file);
      const newItem: EditorImageItem = {
        id: `new-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`,
        isExisting: false,
        file: file,
        previewUrl: previewUrl,
        fileName: file.name,
        fileSize: file.size,
        displayOrder: this.items.length + 1,
      };

      this.items.push(newItem);
    }

    this.recalculateOrders();
  }

  removeImage(index: number): void {
    if (index < 0 || index >= this.items.length) return;
    const item = this.items[index];

    if (item.isExisting && item.encryptedImageId) {
      if (!this.removedImageIds.includes(item.encryptedImageId)) {
        this.removedImageIds.push(item.encryptedImageId);
      }
    } else if (item.previewUrl && item.previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(item.previewUrl);
    }

    this.items.splice(index, 1);
    this.recalculateOrders();
  }

  moveUp(index: number): void {
    if (index <= 0) return;
    const temp = this.items[index];
    this.items[index] = this.items[index - 1];
    this.items[index - 1] = temp;
    this.recalculateOrders();
  }

  moveDown(index: number): void {
    if (index >= this.items.length - 1) return;
    const temp = this.items[index];
    this.items[index] = this.items[index + 1];
    this.items[index + 1] = temp;
    this.recalculateOrders();
  }

  // HTML5 Drag-and-drop thumbnail reordering
  onItemDragStart(event: DragEvent, index: number): void {
    this.draggedIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', index.toString());
    }
  }

  onItemDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    this.dragOverIndex = index;
  }

  onItemDrop(event: DragEvent, targetIndex: number): void {
    event.preventDefault();
    if (this.draggedIndex !== null && this.draggedIndex !== targetIndex) {
      const movedItem = this.items.splice(this.draggedIndex, 1)[0];
      this.items.splice(targetIndex, 0, movedItem);
      this.recalculateOrders();
    }
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  onItemDragEnd(): void {
    this.draggedIndex = null;
    this.dragOverIndex = null;
  }

  private recalculateOrders(): void {
    this.items.forEach((item, index) => {
      item.displayOrder = index + 1;
    });

    const newFiles = this.items.filter((it) => !it.isExisting && it.file).map((it) => it.file!);

    const imageOrders: ProjectImageOrderDto[] = this.items
      .filter((it) => it.isExisting && it.encryptedImageId)
      .map((it) => ({
        encryptedImageId: it.encryptedImageId!,
        displayOrder: it.displayOrder,
      }));

    queueMicrotask(() => {
      if (!this.isDestroyed) {
        this.imagesChanged.emit({
          allImages: [...this.items],
          newFiles,
          removeImageIds: [...this.removedImageIds],
          imageOrders,
        });
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
