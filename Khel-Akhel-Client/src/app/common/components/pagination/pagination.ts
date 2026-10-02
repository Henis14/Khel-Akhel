import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.html',
  styleUrl: './pagination.scss'
})
export class PaginationComponent {
  @Input() pageIndex: number = 1;
  @Input() pageSize: number = 10;
  @Input() totalRecords: number = 0;

  @Output() pageChange = new EventEmitter<number>();

  protected Math = Math;

  get totalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize) || 1;
  }

  get startRecord(): number {
    return this.totalRecords === 0 ? 0 : (this.pageIndex - 1) * this.pageSize + 1;
  }

  get endRecord(): number {
    return Math.min(this.pageIndex * this.pageSize, this.totalRecords);
  }

  get pages(): number[] {
    const pagesList: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, this.pageIndex - 2);
    let end = Math.min(this.totalPages, start + maxVisible - 1);

    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) {
      pagesList.push(i);
    }
    return pagesList;
  }

  onPageClick(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.pageIndex) {
      this.pageChange.emit(page);
    }
  }
}
