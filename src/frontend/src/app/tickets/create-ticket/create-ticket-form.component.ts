import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { distinctUntilChanged } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TicketService, CreateTicketPayload } from '../ticket.service';
import { FieldDefinition, FieldDefinitionService } from '../field-definition.service';
import { Customer, CustomerService } from '../../customers/services/customer.service';
import { Department, DepartmentService } from '../../admin/departments/department.service';
import { Category, CategoryService } from '../../admin/categories/category.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-create-ticket-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './create-ticket-form.component.html',
})
export class CreateTicketFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly fieldDefService = inject(FieldDefinitionService);
  private readonly customerService = inject(CustomerService);
  private readonly departmentService = inject(DepartmentService);
  private readonly categoryService = inject(CategoryService);
  protected readonly router = inject(Router);

  customers: Customer[] = [];
  departments: Department[] = [];
  categories: Category[] = [];
  customersLoading = true;
  dropdownsLoading = true;
  customFieldDefs: FieldDefinition[] = [];
  submitting = false;

  form = this.fb.group({
    customerId: ['', Validators.required],
    departmentId: ['', Validators.required],
    categoryId: [''],
    subject: ['', [Validators.required, Validators.minLength(3)]],
    subjectAr: ['', Validators.required],
    description: ['', Validators.required],
    descriptionAr: ['', Validators.required],
    priority: ['Medium', Validators.required],
    customFields: this.fb.array([]),
  });

  get customFieldsArray(): FormArray {
    return this.form.get('customFields') as FormArray;
  }

  ngOnInit(): void {
    this.customerService.list({ page: 1, pageSize: 200, isActive: true }).subscribe({
      next: res => {
        this.customers = res.items;
        this.customersLoading = false;
      },
      error: () => { this.customersLoading = false; },
    });

    forkJoin({
      depts: this.departmentService.list(),
      cats: this.categoryService.list(),
    }).subscribe({
      next: ({ depts, cats }) => {
        this.departments = (depts.data ?? []).filter(d => d.isActive);
        this.categories = this.flattenCategories(cats.data ?? []).filter(c => c.isActive);
        this.dropdownsLoading = false;
      },
      error: () => { this.dropdownsLoading = false; },
    });

    this.form.get('departmentId')!.valueChanges.pipe(
      distinctUntilChanged(),
    ).subscribe(deptId => {
      if (deptId) this.loadCustomFields(deptId);
    });
  }

  private flattenCategories(cats: Category[]): Category[] {
    return cats.flatMap(c => [c, ...(c.children ? this.flattenCategories(c.children) : [])]);
  }

  private loadCustomFields(departmentId: string): void {
    this.fieldDefService.list(departmentId).subscribe(defs => {
      this.customFieldDefs = defs;
      this.customFieldsArray.clear();
      defs.forEach(def => {
        this.customFieldsArray.push(
          this.fb.group({
            definitionId: [def.id],
            value: ['', def.required ? Validators.required : []],
          }),
        );
      });
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const val = this.form.getRawValue() as unknown as CreateTicketPayload;
    this.ticketService.create(val).subscribe({
      next: ticket => this.router.navigate(['/app/tickets', ticket.id]),
      error: () => (this.submitting = false),
    });
  }
}
