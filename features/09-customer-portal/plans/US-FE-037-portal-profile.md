# Portal Profile — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-037
**Goal:** Implement the customer portal profile page at `/portal/profile` — displays customer details with inline edit mode triggered by a pencil icon, email field is read-only with a lock icon and tooltip, and all changes are saved via PATCH.

**Architecture:** `PortalProfileService` wraps GET and PATCH `/api/v1/portal/profile`. `PortalProfileComponent` is standalone, lazy-loaded. It uses a `signal<PortalProfile | null>` for the loaded profile, a `signal<boolean>` for edit mode, and a reactive form that is only enabled in edit mode. On save, the PATCH response replaces the signal value. Email is always `[disabled]` in the form.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/services/portal-profile.service.ts` |
| Create | `src/app/portal/services/portal-profile.service.spec.ts` |
| Create | `src/app/portal/profile/portal-profile.component.ts` |
| Create | `src/app/portal/profile/portal-profile.component.html` |
| Create | `src/app/portal/profile/portal-profile.component.spec.ts` |

---

## Task 1: PortalProfileService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/portal-profile.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PortalProfileService } from './portal-profile.service';

describe('PortalProfileService', () => {
  let service: PortalProfileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PortalProfileService],
    });
    service = TestBed.inject(PortalProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('get() should GET /api/v1/portal/profile', () => {
    service.get().subscribe();
    const req = httpMock.expectOne('/api/v1/portal/profile');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'c1', fullName: 'Jane Doe', email: 'jane@example.com', phone: '' });
  });

  it('update() should PATCH /api/v1/portal/profile', () => {
    service.update({ fullName: 'Jane Smith', phone: '555-1234' }).subscribe();
    const req = httpMock.expectOne('/api/v1/portal/profile');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body.fullName).toBe('Jane Smith');
    req.flush({ id: 'c1', fullName: 'Jane Smith', email: 'jane@example.com', phone: '555-1234' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/portal-profile.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/portal-profile.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PortalProfile {
  id: string;
  fullName: string;
  fullNameAr?: string;
  email: string;
  phone?: string;
  preferredLanguage?: string;
  companyName?: string;
  companyNameAr?: string;
}

@Injectable({ providedIn: 'root' })
export class PortalProfileService {
  private readonly http = inject(HttpClient);

  get(): Observable<PortalProfile> {
    return this.http.get<PortalProfile>('/api/v1/portal/profile');
  }

  update(payload: Partial<Omit<PortalProfile, 'id' | 'email'>>): Observable<PortalProfile> {
    return this.http.patch<PortalProfile>('/api/v1/portal/profile', payload);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/portal-profile.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/portal-profile.service.ts src/app/portal/services/portal-profile.service.spec.ts
git commit -m "feat(portal): add PortalProfileService (US-FE-037)"
```

---

## Task 2: PortalProfileComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/profile/portal-profile.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { PortalProfileComponent } from './portal-profile.component';
import { PortalProfileService, PortalProfile } from '../services/portal-profile.service';

const mockProfile: PortalProfile = {
  id: 'c1',
  fullName: 'Jane Doe',
  email: 'jane@example.com',
  phone: '555-0000',
  companyName: 'ACME Corp',
};

describe('PortalProfileComponent', () => {
  let fixture: ComponentFixture<PortalProfileComponent>;
  let component: PortalProfileComponent;
  let profileService: jasmine.SpyObj<PortalProfileService>;

  beforeEach(async () => {
    profileService = jasmine.createSpyObj('PortalProfileService', ['get', 'update']);
    profileService.get.and.returnValue(of(mockProfile));
    profileService.update.and.returnValue(of({ ...mockProfile, fullName: 'Jane Smith' }));

    await TestBed.configureTestingModule({
      imports: [PortalProfileComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: PortalProfileService, useValue: profileService }],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load profile on init', () => {
    expect(profileService.get).toHaveBeenCalled();
    expect(component.profile()).toEqual(mockProfile);
  });

  it('should populate form with profile values', () => {
    expect(component.form.get('fullName')!.value).toBe('Jane Doe');
  });

  it('email field should always be disabled', () => {
    expect(component.form.get('email')!.disabled).toBeTrue();
  });

  it('form should be disabled in view mode', () => {
    expect(component.editMode()).toBeFalse();
    expect(component.form.get('fullName')!.disabled).toBeTrue();
  });

  it('should enable form fields when edit mode is activated', () => {
    component.enterEditMode();
    expect(component.editMode()).toBeTrue();
    expect(component.form.get('fullName')!.disabled).toBeFalse();
  });

  it('should PATCH profile on save', () => {
    component.enterEditMode();
    component.form.get('fullName')!.setValue('Jane Smith');
    component.save();
    expect(profileService.update).toHaveBeenCalledWith(jasmine.objectContaining({ fullName: 'Jane Smith' }));
  });

  it('should exit edit mode after successful save', () => {
    component.enterEditMode();
    component.save();
    expect(component.editMode()).toBeFalse();
  });

  it('should cancel edit mode without saving', () => {
    component.enterEditMode();
    component.form.get('fullName')!.setValue('Changed Name');
    component.cancelEdit();
    expect(component.editMode()).toBeFalse();
    expect(component.form.get('fullName')!.value).toBe('Jane Doe');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/profile/portal-profile.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/profile/portal-profile.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PortalProfileService, PortalProfile } from '../services/portal-profile.service';

@Component({
  selector: 'app-portal-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './portal-profile.component.html',
})
export class PortalProfileComponent implements OnInit {
  private readonly profileService = inject(PortalProfileService);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<PortalProfile | null>(null);
  readonly editMode = signal(false);
  readonly saving = signal(false);

  form = this.fb.group({
    fullName: [{ value: '', disabled: true }],
    fullNameAr: [{ value: '', disabled: true }],
    email: [{ value: '', disabled: true }],
    phone: [{ value: '', disabled: true }],
    companyName: [{ value: '', disabled: true }],
    companyNameAr: [{ value: '', disabled: true }],
  });

  ngOnInit(): void {
    this.profileService.get().subscribe(p => {
      this.profile.set(p);
      this.form.patchValue({ fullName: p.fullName, fullNameAr: p.fullNameAr ?? '', email: p.email, phone: p.phone ?? '', companyName: p.companyName ?? '', companyNameAr: p.companyNameAr ?? '' });
    });
  }

  enterEditMode(): void {
    this.editMode.set(true);
    this.form.get('fullName')!.enable();
    this.form.get('fullNameAr')!.enable();
    this.form.get('phone')!.enable();
    this.form.get('companyName')!.enable();
    this.form.get('companyNameAr')!.enable();
  }

  cancelEdit(): void {
    const p = this.profile();
    if (p) this.form.patchValue({ fullName: p.fullName, fullNameAr: p.fullNameAr ?? '', phone: p.phone ?? '', companyName: p.companyName ?? '', companyNameAr: p.companyNameAr ?? '' });
    this.editMode.set(false);
    this.form.get('fullName')!.disable();
    this.form.get('fullNameAr')!.disable();
    this.form.get('phone')!.disable();
    this.form.get('companyName')!.disable();
    this.form.get('companyNameAr')!.disable();
  }

  save(): void {
    this.saving.set(true);
    const val = this.form.getRawValue();
    this.profileService.update({ fullName: val.fullName!, fullNameAr: val.fullNameAr || undefined, phone: val.phone!, companyName: val.companyName!, companyNameAr: val.companyNameAr || undefined }).subscribe(updated => {
      this.profile.set(updated);
      this.saving.set(false);
      this.editMode.set(false);
      this.form.get('fullName')!.disable();
      this.form.get('fullNameAr')!.disable();
      this.form.get('phone')!.disable();
      this.form.get('companyName')!.disable();
      this.form.get('companyNameAr')!.disable();
    });
  }
}
```

```html
<!-- src/app/portal/profile/portal-profile.component.html -->

<div class="p-6 max-w-xl mx-auto">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-semibold">My Profile</h1>
    @if (!editMode()) {
      <button mat-icon-button (click)="enterEditMode()" matTooltip="Edit profile">
        <mat-icon>edit</mat-icon>
      </button>
    }
  </div>

  @if (profile()) {
    <form [formGroup]="form" class="flex flex-col gap-4">

      <mat-form-field appearance="outline">
        <mat-label>Full Name</mat-label>
        <input matInput formControlName="fullName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Full Name (Arabic)</mat-label>
        <input matInput formControlName="fullNameAr" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Email</mat-label>
        <input matInput formControlName="email" />
        <mat-icon matSuffix matTooltip="Email cannot be changed here. Contact support to update your email.">lock</mat-icon>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Phone</mat-label>
        <input matInput formControlName="phone" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Company Name</mat-label>
        <input matInput formControlName="companyName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Company Name (Arabic)</mat-label>
        <input matInput formControlName="companyNameAr" />
      </mat-form-field>

      @if (editMode()) {
        <div class="flex gap-3 justify-end">
          <button mat-stroked-button type="button" (click)="cancelEdit()">Cancel</button>
          <button mat-raised-button color="primary" type="button" (click)="save()" [disabled]="saving()">
            {{ saving() ? 'Saving…' : 'Save Changes' }}
          </button>
        </div>
      }
    </form>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/profile/portal-profile.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/profile/
git commit -m "feat(portal): implement PortalProfileComponent with inline edit mode (US-FE-037)"
```
