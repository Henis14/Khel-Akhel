import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';
import { UserHomeComponent } from './home';
import { ApiService } from '../../../common/services/api.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('UserHomeComponent', () => {
  let component: UserHomeComponent;
  let fixture: ComponentFixture<UserHomeComponent>;
  let mockApiService: any;

  const mockActiveImages = [
    {
      id: 10,
      encryptedId: 'enc-10',
      title: 'Dynamic Hero 1',
      mediaUrl: '/uploads/hero-banner/hero1.jpg',
      isImage: true,
      isVideo: false,
      displayOrder: 1,
      isActive: true,
      createdAt: '2026-10-23 10:00:00',
    },
    {
      id: 11,
      encryptedId: 'enc-11',
      title: 'Dynamic Hero 2',
      mediaUrl: '/uploads/hero-banner/hero2.jpg',
      isImage: true,
      isVideo: false,
      displayOrder: 2,
      isActive: true,
      createdAt: '2026-10-23 10:05:00',
    },
  ];

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn().mockReturnValue(of({ success: true, data: mockActiveImages })),
    };

    await TestBed.configureTestingModule({
      imports: [UserHomeComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ApiService, useValue: mockApiService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component and fetch hero media', () => {
    expect(component).toBeTruthy();
    expect(mockApiService.get).toHaveBeenCalledWith('hero-banner');
    expect(component.heroMedia().length).toBe(2);
    expect(component.isImageMode()).toBe(true);
    expect(component.isVideoMode()).toBe(false);
  });

  it('should compute background url from dynamic active hero media', () => {
    const bgUrl = component.currentBackgroundUrl();
    expect(bgUrl).toContain('/uploads/hero-banner/hero1.jpg');
  });

  it('should not render any static hero text or legacy copy in the template', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const heroSection = compiled.querySelector('.home-hero');
    expect(heroSection).toBeTruthy();

    // Verify static content is completely absent from hero
    expect(heroSection?.textContent).not.toContain('A creative world by DARAS');
    expect(heroSection?.textContent).not.toContain('A little roomfor wonder');
    expect(heroSection?.textContent).not.toContain(
      'Thoughtful objects, art and experiences shaped by culture',
    );
    expect(heroSection?.textContent).not.toContain('Explore the collection');
    expect(heroSection?.textContent).not.toContain('Art · Objects · Gatherings · Play');

    // Verify legacy shade overlay and caption classes are gone
    expect(compiled.querySelector('.home-hero-shade')).toBeNull();
    expect(compiled.querySelector('.home-hero-content')).toBeNull();
    expect(compiled.querySelector('.home-hero-caption')).toBeNull();
  });

  it('should navigate through slides when carousel buttons are clicked', () => {
    expect(component.activeSlideIndex()).toBe(0);

    component.nextSlide();
    expect(component.activeSlideIndex()).toBe(1);

    component.nextSlide();
    expect(component.activeSlideIndex()).toBe(0);

    component.prevSlide();
    expect(component.activeSlideIndex()).toBe(1);

    component.goToSlide(0);
    expect(component.activeSlideIndex()).toBe(0);
  });

  it('should handle video banner correctly', () => {
    const mockVideo = [
      {
        id: 20,
        encryptedId: 'enc-20',
        title: 'Hero Video',
        mediaUrl: '/uploads/hero-banner/video.mp4',
        isImage: false,
        isVideo: true,
        displayOrder: 1,
        isActive: true,
        createdAt: '2026-10-23 10:10:00',
      },
    ];

    component.heroMedia.set(mockVideo);
    fixture.detectChanges();

    expect(component.isVideoMode()).toBe(true);
    expect(component.isImageMode()).toBe(false);
    expect(component.videoBanner()?.mediaUrl).toBe('/uploads/hero-banner/video.mp4');

    const videoEl = fixture.nativeElement.querySelector('video.home-hero-video');
    expect(videoEl).toBeTruthy();
  });
});
