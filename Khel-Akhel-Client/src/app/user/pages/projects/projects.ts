import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UserProjectService } from '../../services/user-project.service';
import { ProjectPublic } from '../../../admin/models/project.model';
import {
  GalleryImageItem,
  GalleryRendererComponent,
} from '../../../common/components/gallery-renderer/gallery-renderer';
import { AppLoaderComponent } from '../../../common/components/app-loader/app-loader';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterModule, GalleryRendererComponent, AppLoaderComponent],
  templateUrl: './projects.html',
  styleUrl: './projects.scss',
})
export class ProjectsComponent implements OnInit {
  private projectService = inject(UserProjectService);
  private cdr = inject(ChangeDetectorRef);

  sections: ProjectPublic[] = [];
  loading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadPublicProjects();
  }

  loadPublicProjects(): void {
    this.loading = true;
    this.errorMessage = null;

    this.projectService.getPublicProjects().subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.sections = res.data;
        } else {
          this.sections = [];
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage =
          err?.error?.message ||
          'Unable to load project archives at this time. Please check your connection and try again.';
        this.cdr.markForCheck();
      },
    });
  }

  getSectionImages(section: ProjectPublic): GalleryImageItem[] {
    if (!section.images) return [];
    return section.images.map((img) => ({
      imageUrl: this.projectService.getImageUrl(img.imageUrl || img.imagePath),
      originalFileName: img.originalFileName,
      displayOrder: img.displayOrder,
    }));
  }

  isValidExternalUrl(url: string | null | undefined): boolean {
    if (!url) return false;
    return url.startsWith('http://') || url.startsWith('https://');
  }

  hasVisibleCredits(credits: string | null | undefined): boolean {
    if (!credits) return false;
    const trimmed = credits.trim();
    if (!trimmed) return false;
    const lower = trimmed.toLowerCase();
    return lower !== 'khel-akhel studio team' && lower !== 'khel akhel studio team';
  }
}
