import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, defer, map, of, startWith } from 'rxjs';
import { ApiService } from '../../../common/services/api.service';
import { AuthService } from '../../../common/services/auth.service';
import { UserProjectService } from '../../services/user-project.service';
import { CustomValidators } from '../../../common/validators/custom.validators';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';
import { validateImageFile } from '../../../common/utils/file-validation.util';

interface CommissionCategory {
  encryptedCategoryId: string;
  categoryName: string;
  description?: string | null;
}

interface CommissionCreated {
  requestNo: string;
  fullName: string;
  categoryName: string;
}

/** Limits enforced by CommissionsController */
const MAX_IMAGES = 3;
const MAX_WORDS = 500;
const MAX_CHARS = 5000;

function countWords(value: string | null | undefined): number {
  return (value || '').trim().split(/\s+/).filter(Boolean).length;
}

function maxWords(limit: number) {
  return (c: AbstractControl): ValidationErrors | null =>
    countWords(c.value) > limit ? { maxWords: true } : null;
}

function notInPast(c: AbstractControl): ValidationErrors | null {
  if (!c.value) return null;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return new Date(`${c.value}T00:00:00`) < today ? { past: true } : null;
}

/**
 * Commission request form, posted as multipart to `commissions/create`
 * (captcha required, up to 3 reference images, honeypot field `website`).
 */
@Component({
  selector: 'app-commissions',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './commissions.html',
  styleUrl: './commissions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommissionsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private destroyRef = inject(DestroyRef);
  private projectService = inject(UserProjectService);

  /** Up to three photographs from published projects, for the opening collage */
  readonly photos = signal<string[]>([]);

  readonly maxImages = MAX_IMAGES;
  readonly maxWords = MAX_WORDS;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly form = this.fb.nonNullable.group({
    fullName: [
      '',
      [CustomValidators.requiredNonEmpty, Validators.minLength(2), Validators.maxLength(150)],
    ],
    mobileNo: ['', [CustomValidators.requiredNonEmpty, Validators.pattern(REGEX_PATTERNS.MOBILE)]],
    email: [
      '',
      [
        CustomValidators.requiredNonEmpty,
        Validators.pattern(REGEX_PATTERNS.EMAIL),
        Validators.maxLength(255),
      ],
    ],
    category: ['', CustomValidators.requiredNonEmpty],
    otherCategoryDescription: ['', Validators.maxLength(255)],
    expectedTimeline: ['', [CustomValidators.requiredNonEmpty, notInPast]],
    projectLocation: [
      '',
      [CustomValidators.requiredNonEmpty, Validators.minLength(2), Validators.maxLength(255)],
    ],
    projectDescription: [
      '',
      [
        CustomValidators.requiredNonEmpty,
        Validators.minLength(5),
        Validators.maxLength(MAX_CHARS),
        maxWords(MAX_WORDS),
      ],
    ],
    keywords: ['', Validators.maxLength(500)],
    captcha: ['', [CustomValidators.requiredNonEmpty, Validators.pattern(/^[A-Za-z0-9]{5}$/)]],
    website: [''], // honeypot, stays empty for people
  });

  readonly categories = signal<CommissionCategory[]>([]);
  readonly images = signal<{ file: File; url: string }[]>([]);
  readonly imageError = signal('');

  readonly captchaId = signal<string | null>(null);
  readonly captchaImage = signal<string | null>(null);
  readonly captchaLoading = signal(false);

  readonly submitting = signal(false);
  readonly errorMessages = signal<string[]>([]);
  readonly created = signal<CommissionCreated | null>(null);

  readonly wordCount = toSignal(
    this.form.controls.projectDescription.valueChanges.pipe(
      startWith(''),
      map((v) => countWords(v)),
    ),
    { initialValue: 0 },
  );

  private readonly categoryValue = toSignal(
    this.form.controls.category.valueChanges.pipe(startWith('')),
    { initialValue: '' },
  );

  readonly isOther = computed(() => {
    const c = this.categories().find((x) => x.encryptedCategoryId === this.categoryValue());
    return !!c && c.categoryName.trim().toLowerCase() === 'others';
  });

  ngOnInit(): void {
    this.prefillFromAccount();
    this.loadCategories();
    this.loadCaptcha();
    this.loadPhotos();
  }

  /** Photographs for the opening collage; the page works the same without them */
  private loadPhotos(): void {
    defer(() => this.projectService.getStoreProjects())
      .pipe(
        map((projects) =>
          (projects || [])
            .map((p) => p.imageUrls?.[0])
            .filter((src): src is string => !!src)
            .slice(0, 3),
        ),
        catchError(() => of([] as string[])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((srcs) => this.photos.set(srcs));
  }

  /** "Start your brief" jumps to the form without changing the address */
  scrollToBrief(event: Event): void {
    event.preventDefault();
    document.getElementById('cmBrief')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  private prefillFromAccount(): void {
    const email = this.auth.currentUser()?.email;
    if (email) this.form.controls.email.setValue(email);
  }

  loadCategories(): void {
    this.api
      .get<CommissionCategory[]>('commissions/categories')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.categories.set(res?.success && Array.isArray(res.data) ? res.data : []),
        error: () => this.categories.set([]),
      });
  }

  loadCaptcha(): void {
    this.captchaLoading.set(true);
    this.auth.getCaptcha().subscribe({
      next: (res) => {
        this.captchaLoading.set(false);
        if (res?.success && res.data) {
          this.captchaId.set(res.data.captchaId);
          this.captchaImage.set(res.data.captchaImage);
        } else {
          this.captchaImage.set(null);
        }
        this.form.controls.captcha.reset('');
      },
      error: () => {
        this.captchaLoading.set(false);
        this.captchaImage.set(null);
      },
    });
  }

  /* ---------- reference images ---------- */
  onFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const picked = Array.from(input.files ?? []);
    input.value = '';
    this.imageError.set('');

    const current = this.images();
    const room = MAX_IMAGES - current.length;
    if (picked.length > room) {
      this.imageError.set(`You can add up to ${MAX_IMAGES} images.`);
    }

    const accepted: { file: File; url: string }[] = [];
    for (const file of picked.slice(0, Math.max(room, 0))) {
      const check = validateImageFile(file);
      if (!check.isValid) {
        this.imageError.set(check.errorMessage ?? 'This file cannot be used.');
        continue;
      }
      accepted.push({ file, url: URL.createObjectURL(file) });
    }
    this.images.set([...current, ...accepted]);
  }

  removeImage(index: number): void {
    const list = [...this.images()];
    const [removed] = list.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.url);
    this.images.set(list);
    this.imageError.set('');
  }

  /* ---------- helpers for the template ---------- */
  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.touched || c.dirty);
  }

  /* ---------- submit ---------- */
  submit(): void {
    this.errorMessages.set([]);

    const other = this.form.controls.otherCategoryDescription;
    if (this.isOther() && !other.value.trim()) {
      other.setErrors({ required: true });
    }

    if (this.form.invalid || !this.captchaId()) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const data = new FormData();
    data.append('FullName', v.fullName.trim());
    data.append('MobileNo', v.mobileNo.trim());
    data.append('Email', v.email.trim());
    data.append('EncryptedCategoryId', v.category);
    if (this.isOther()) data.append('OtherCategoryDescription', v.otherCategoryDescription.trim());
    data.append('ExpectedTimeline', v.expectedTimeline);
    data.append('ProjectLocation', v.projectLocation.trim());
    data.append('ProjectDescription', v.projectDescription.trim());
    if (v.keywords.trim()) data.append('Keywords', v.keywords.trim());
    data.append('CaptchaId', this.captchaId() ?? '');
    data.append('Captcha', v.captcha.trim().toUpperCase());
    data.append('Website', v.website);
    this.images().forEach((img) => data.append('Images', img.file, img.file.name));

    this.submitting.set(true);
    this.api.post<CommissionCreated>('commissions/create', data).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (res?.success) {
          this.created.set(res.data);
          this.images().forEach((img) => URL.revokeObjectURL(img.url));
          this.images.set([]);
          this.form.reset();
          window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
          this.errorMessages.set(
            res?.errors && res.errors.length
              ? res.errors
              : ['Something went wrong. Please try again.'],
          );
          this.loadCaptcha();
        }
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.errorMessages.set(this.readErrors(err));
        this.loadCaptcha(); // captcha is single use
      },
    });
  }

  private readErrors(err: unknown): string[] {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as { message?: string; errors?: string[] } | null;
      if (body?.message === 'INVALID_CAPTCHA')
        return ['The security code did not match. Please try the new one.'];
      if (body?.errors && body.errors.length) return body.errors;
      if (err.status === 429) return ['Too many requests. Please wait a minute and try again.'];
    }
    return ['We could not send your request. Please check your connection and try again.'];
  }

  startAnother(): void {
    this.created.set(null);
    this.prefillFromAccount();
    this.loadCaptcha();
  }
}
