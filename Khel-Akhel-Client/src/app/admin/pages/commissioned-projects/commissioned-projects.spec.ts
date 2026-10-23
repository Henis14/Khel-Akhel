import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';
import { CommissionedProjectsComponent } from './commissioned-projects';
import { AdminCommissionedProjectService } from '../../services/admin-commissioned-project.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('CommissionedProjectsComponent', () => {
  let component: CommissionedProjectsComponent;
  let fixture: ComponentFixture<CommissionedProjectsComponent>;
  let mockCommissionService: any;

  const mockCategoriesResponse = {
    success: true,
    data: [
      { encryptedCategoryId: 'cat-1', categoryName: 'Wall Murals' },
      { encryptedCategoryId: 'cat-2', categoryName: 'Sculptures' },
    ],
  };

  const mockListResponse = {
    success: true,
    data: {
      items: [
        {
          encryptedRequestId: 'req-1',
          requestNo: 'KA-COM-202610-001',
          customerName: 'Aarav Mehta',
          customerEmail: 'aarav@example.com',
          customerMobile: '+91 9876543210',
          categoryName: 'Wall Murals',
          otherCategoryDescription: null,
          projectLocation: 'Mumbai',
          expectedTimeline: '2026-11-15',
          status: 'New',
          createdDate: '11 Oct 2026',
          imageCount: 2,
          thumbnailUrl: '/uploads/thumb.jpg',
        },
      ],
      totalRecords: 1,
      pageIndex: 1,
      pageSize: 10,
    },
  };

  beforeEach(async () => {
    mockCommissionService = {
      getCategories: vi.fn().mockReturnValue(of(mockCategoriesResponse)),
      getList: vi.fn().mockReturnValue(of(mockListResponse)),
      getById: vi.fn().mockReturnValue(of({ success: true, data: null })),
      updateStatus: vi.fn().mockReturnValue(of({ success: true, data: null })),
      getImageUrl: vi.fn().mockImplementation((path: string) => path || ''),
    };

    await TestBed.configureTestingModule({
      imports: [CommissionedProjectsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AdminCommissionedProjectService, useValue: mockCommissionService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CommissionedProjectsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component and load initial data', () => {
    expect(component).toBeTruthy();
    expect(mockCommissionService.getCategories).toHaveBeenCalled();
    expect(mockCommissionService.getList).toHaveBeenCalled();
    expect(component.requests.length).toBe(1);
    expect(component.totalRecords).toBe(1);
  });

  it('should reload the list when applyFilters is called', () => {
    mockCommissionService.getList.mockClear();
    component.pageIndex = 3;

    component.applyFilters();
    expect(component.pageIndex).toBe(1);
    expect(mockCommissionService.getList).toHaveBeenCalled();
  });

  it('should identify active filters accurately', () => {
    expect(component.hasActiveFilters).toBe(false);

    component.searchQuery = 'mural';
    expect(component.hasActiveFilters).toBe(true);
    component.searchQuery = '';

    component.selectedCategory = 'cat-1';
    expect(component.hasActiveFilters).toBe(true);
    component.selectedCategory = '';

    component.selectedStatus = 'New';
    expect(component.hasActiveFilters).toBe(true);
    component.selectedStatus = '';

    component.sortBy = 'oldest';
    expect(component.hasActiveFilters).toBe(true);
    component.sortBy = 'newest';
    expect(component.hasActiveFilters).toBe(false);
  });

  it('should reset all filters and reload list when resetFilters is called', () => {
    component.searchQuery = 'mural';
    component.selectedCategory = 'cat-1';
    component.selectedStatus = 'New';
    component.sortBy = 'oldest';
    component.pageIndex = 3;

    mockCommissionService.getList.mockClear();
    component.resetFilters();

    expect(component.searchQuery).toBe('');
    expect(component.selectedCategory).toBe('');
    expect(component.selectedStatus).toBe('');
    expect(component.sortBy).toBe('newest');
    expect(component.pageIndex).toBe(1);
    expect(mockCommissionService.getList).toHaveBeenCalled();
  });

  it('should update filters and reset pageIndex to 1', () => {
    mockCommissionService.getList.mockClear();
    component.pageIndex = 2;

    component.onCategoryFilter('cat-2');
    expect(component.selectedCategory).toBe('cat-2');
    expect(component.pageIndex).toBe(1);
    expect(mockCommissionService.getList).toHaveBeenCalled();

    mockCommissionService.getList.mockClear();
    component.pageIndex = 3;

    component.onStatusFilter('In Review');
    expect(component.selectedStatus).toBe('In Review');
    expect(component.pageIndex).toBe(1);
    expect(mockCommissionService.getList).toHaveBeenCalled();

    mockCommissionService.getList.mockClear();
    component.onSortChange('timeline_asc');
    expect(component.sortBy).toBe('timeline_asc');
    expect(component.pageIndex).toBe(1);
    expect(mockCommissionService.getList).toHaveBeenCalled();
  });
});
