import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { TicketService, CreateTicketPayload } from '../ticket.service';
import { FieldDefinition, FieldDefinitionService } from '../field-definition.service';
import { CustomerService } from '../../customers/services/customer.service';

@Component({
  selector: 'app-create-ticket-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './create-ticket-form.component.html',
})
export class CreateTicketFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly fieldDefService = inject(FieldDefinitionService);
  private readonly customerService = inject(CustomerService);
  protected readonly router = inject(Router);

  customFieldDefs: FieldDefinition[] = [];
  customerSuggestions: { id: string; label: string }[] = [];
  submitting = false;

  form = this.fb.group({
    customerId: ['', Validators.required],
    departmentId: ['', Validators.required],
    categoryId: [''],
    subject: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', Validators.required],
    priority: ['Medium', Validators.required],
    customFields: this.fb.array([]),
  });

  get customFieldsArray(): FormArray {
    return this.form.get('customFields') as FormArray;
  }

  ngOnInit(): void {
    this.form.get('departmentId')!.valueChanges.pipe(
      distinctUntilChanged(),
    ).subscribe(deptId => {
      if (deptId) this.loadCustomFields(deptId);
    });

    this.form.get('customerId')!.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(search =>
        this.customerService.list({ page: 1, pageSize: 10, search: search ?? '' })
      ),
    ).subscribe(res => {
      this.customerSuggestions = res.items.map(c => ({
        id: c.id,
        label: `${c.fullName} — ${c.email}`,
      }));
    });
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
