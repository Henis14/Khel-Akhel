import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';
import { HeroBannerComponent } from './hero-banner';
import { AdminHeroBannerService } from '../../services/admin-hero-banner.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('HeroBannerComponent', () => {
  let component: HeroBannerComponent;
  let fixture: ComponentFixture<HeroBannerComponent>;
  let mockHeroService: any;

  const mockBannerList = [
    {
      id: 1,
      encryptedId: 'enc-1',
      title: 'Summer Collection',
      mediaUrl: '/uploads/hero-banner/img1.png',
      isImage: true,
      isVideo: false,
      displayOrder: 1,
      isActive: true,
      createdAt: '2026-10-23 10:00:00',
    },
    {
      id: 2,
      encryptedId: 'enc-2',
      title: 'Objects of Wonder',
      mediaUrl: '/uploads/hero-banner/img2.png',
      isImage: true,
      isVideo: false,
      displayOrder: 2,
      isActive: false,
      createdAt: '2026-10-23 10:05:00',
    },
  ];

  beforeEach(async () => {
    mockHeroService = {
      getAdminList: vi.fn().mockReturnValue(of({ success: true, data: mockBannerList })),
      createImages: vi.fn().mockReturnValue(of({ success: true, data: mockBannerList })),
      createVideo: vi.fn().mockReturnValue(of({ success: true, data: [] })),
      update: vi.fn().mockReturnValue(of({ success: true, data: mockBannerList[0] })),
      toggleStatus: vi.fn().mockReturnValue(of({ success: true, data: mockBannerList[0] })),
      delete: vi.fn().mockReturnValue(of({ success: true, data: null })),
      getPublic: vi.fn().mockReturnValue(of({ success: true, data: mockBannerList })),
    };

    await TestBed.configureTestingModule({
      imports: [HeroBannerComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AdminHeroBannerService, useValue: mockHeroService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeroBannerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component and load existing banners', () => {
    expect(component).toBeTruthy();
    expect(mockHeroService.getAdminList).toHaveBeenCalled();
    expect(component.banners().length).toBe(2);
    expect(component.currentConfigurationType).toBe('images');
    expect(component.activeBannerCount).toBe(1);
  });

  it('should identify video configuration type correctly', () => {
    component.banners.set([
      {
        id: 10,
        encryptedId: 'enc-10',
        title: 'Brand Reel',
        mediaUrl: '/uploads/hero-banner/reel.mp4',
        isImage: false,
        isVideo: true,
        displayOrder: 1,
        isActive: true,
        createdAt: '2026-10-23 11:00:00',
      },
    ]);

    expect(component.currentConfigurationType).toBe('video');
  });

  it('should toggle active status via service', () => {
    const item = { ...mockBannerList[0] };
    component.toggleActive(item);

    expect(mockHeroService.toggleStatus).toHaveBeenCalledWith('enc-1', false);
    expect(item.isActive).toBe(false);
  });

  it('should open delete confirmation dialog and delete on confirmation', () => {
    component.confirmDelete(mockBannerList[0]);
    expect(component.showDeleteDialog).toBe(true);
    expect(component.itemToDelete).toBe(mockBannerList[0]);

    component.onDeleteConfirmed();
    expect(mockHeroService.delete).toHaveBeenCalledWith('enc-1');
  });

  it('should reject non-mp4 video selection in onVideoSelected', () => {
    const fakeFile = new File(['fake data'], 'video.mov', { type: 'video/quicktime' });
    const event = {
      target: { files: [fakeFile], value: '' },
    } as any;

    component.onVideoSelected(event);
    expect(component.uploadValidationError).toContain('Only MP4 videos are allowed');
    expect(component.selectedVideo).toBeNull();
  });

  it('should require replaceExisting when switching from images to video mode', () => {
    component.uploadMode = 'video';
    component.selectedVideo = new File(['data'], 'test.mp4', { type: 'video/mp4' });
    component.uploadReplaceExisting = false;

    component.submitUpload();
    expect(component.uploadValidationError).toContain('Image banners are currently active');
    expect(mockHeroService.createVideo).not.toHaveBeenCalled();
  });
});
