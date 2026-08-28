import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { I18nService } from './shared/services/i18n.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  host: {
    '[attr.lang]': 'i18n.lang()',
    '[attr.dir]': 'i18n.dir()',
  },
})
export class App implements OnInit {
  readonly i18n = inject(I18nService);

  ngOnInit(): void {
    this.i18n.init();
  }
}
