import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminFaqService } from '../../../admin/services/admin-faq.service';
import { FaqResponse } from '../../../admin/models/faq.model';

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './faq.html',
  styleUrl: './faq.scss',
})
export class Faq implements OnInit {
  private faqService = inject(AdminFaqService);
  private cdr = inject(ChangeDetectorRef);

  faqs: FaqResponse[] = [];
  filteredFaqs: FaqResponse[] = [];
  categories: string[] = ['All'];
  selectedCategory: string = 'All';
  searchQuery: string = '';

  loading: boolean = false;
  errorMessage: string | null = null;
  expandedIndices: Set<number> = new Set();

  ngOnInit(): void {
    this.loadFaqs();
  }

  loadFaqs(): void {
    this.loading = true;
    this.errorMessage = null;

    this.faqService.getPublic('faq').subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.faqs = res.data;
          this.extractCategories(res.data);
          this.filterFaqs();
        } else {
          this.faqs = [];
          this.filteredFaqs = [];
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Unable to load FAQs at this time.';
        this.faqs = [];
        this.filteredFaqs = [];
        this.cdr.markForCheck();
      },
    });
  }

  private extractCategories(faqs: FaqResponse[]): void {
    const cats = new Set<string>();
    faqs.forEach((f) => {
      if (f.category && f.category.trim().length > 0) {
        cats.add(f.category.trim());
      }
    });
    this.categories = ['All', ...Array.from(cats).sort((a, b) => a.localeCompare(b))];
  }

  selectCategory(category: string): void {
    this.selectedCategory = category;
    this.filterFaqs();
  }

  onSearch(): void {
    this.filterFaqs();
  }

  filterFaqs(): void {
    let result = [...this.faqs];
    if (this.selectedCategory !== 'All') {
      result = result.filter(
        (f) => (f.category || '').toLowerCase() === this.selectedCategory.toLowerCase(),
      );
    }
    if (this.searchQuery && this.searchQuery.trim().length > 0) {
      const q = this.searchQuery.trim().toLowerCase();
      result = result.filter(
        (f) =>
          f.question.toLowerCase().includes(q) ||
          f.answer.toLowerCase().includes(q) ||
          (f.category || '').toLowerCase().includes(q),
      );
    }
    this.filteredFaqs = result;
  }

  toggleAccordion(index: number): void {
    if (this.expandedIndices.has(index)) {
      this.expandedIndices.delete(index);
    } else {
      this.expandedIndices.add(index);
    }
  }

  isExpanded(index: number): boolean {
    return this.expandedIndices.has(index);
  }
}
