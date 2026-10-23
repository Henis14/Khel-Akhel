import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, startWith } from 'rxjs';
import { ApiService } from '../../../common/services/api.service';
import { AuthService } from '../../../common/services/auth.service';
import { REGEX_PATTERNS } from '../../../common/constants/regex.constants';

interface ContactSubmitted {
  fullName: string;
  email: string;
  confirmationMessage: string;
}

/**
 * Shown on the map and under the studio details.
 * TODO: replace with the exact studio address when confirmed.
 */
const STUDIO_LOCATION = 'Patiala, Punjab, India';
const MAX_MESSAGE = 300;

/** Same rule as ValidationHelper.IsValidInternationalMobile on the API */
function internationalMobile(c: AbstractControl): ValidationErrors | null {
  const v = String(c.value ?? '').trim();
  if (!v) return null;
  const digits = (v.match(/\d/g) ?? []).length;
  return /^\+?[0-9\s\-().]{7,20}$/.test(v) && digits >= 7 && digits <= 15 ? null : { mobile: true };
}

/**
 * Contact page: inquiry form posted as JSON to `contact-us/submit`
 * (ContactUsController, honeypot field `website`, rate limited per IP),
 * studio details and a map.
 */
@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Contact {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  readonly studioLocation = STUDIO_LOCATION;
  readonly maxMessage = MAX_MESSAGE;

  /** keyless Google Maps embed */
  readonly mapUrl: SafeResourceUrl = this.sanitizer.bypassSecurityTrustResourceUrl(
    `https://www.google.com/maps?q=${encodeURIComponent(STUDIO_LOCATION)}&z=12&output=embed`,
  );
  readonly mapLink = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(STUDIO_LOCATION)}`;

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    mobileNumber: ['', [Validators.required, internationalMobile]],
    email: ['', [Validators.required, Validators.pattern(REGEX_PATTERNS.EMAIL), Validators.maxLength(254)]],
    companyName: ['', Validators.maxLength(200)],
    serviceDescription: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(MAX_MESSAGE)]],
    website: [''], // honeypot
  });

  readonly messageLength = toSignal(
    this.form.controls.serviceDescription.valueChanges.pipe(
      startWith(''),
      map((v) => (v || '').length),
    ),
    { initialValue: 0 },
  );

  readonly submitting = signal(false);
  readonly errors = signal<string[]>([]);
  readonly sent = signal<ContactSubmitted | null>(null);

  constructor() {
    const email = this.auth.currentUser()?.email;
    if (email) this.form.controls.email.setValue(email);
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.touched || c.dirty);
  }

  submit(): void {
    this.errors.set([]);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const body = {
      fullName: v.fullName.trim(),
      mobileNumber: v.mobileNumber.trim(),
      email: v.email.trim(),
      companyName: v.companyName.trim() || null,
      serviceDescription: v.serviceDescription.trim(),
      website: v.website,
    };

    this.submitting.set(true);
    this.api.post<ContactSubmitted>('contact-us/submit', body).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (res?.success) {
          this.sent.set(res.data);
          this.form.reset();
        } else {
          this.errors.set(res?.errors && res.errors.length ? res.errors : ['Something went wrong. Please try again.']);
        }
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.errors.set(this.readErrors(err));
      },
    });
  }

  private readErrors(err: unknown): string[] {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 429) return ['You have sent a few messages already. Please try again in a few minutes.'];
      const body = err.error as { errors?: string[] } | null;
      if (body?.errors && body.errors.length) return body.errors;
    }
    return ['We could not send your message. Please check your connection and try again.'];
  }

  sendAnother(): void {
    this.sent.set(null);
    const email = this.auth.currentUser()?.email;
    if (email) this.form.controls.email.setValue(email);
  }
}
