import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SvgDefinitionsComponent } from './shared/svg-definitions.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SvgDefinitionsComponent],
  template: `
    <!-- Global SVG definitions for cards and chips -->
    <app-svg-definitions />

    <!-- Router outlet for page content -->
    <router-outlet />
  `,
  styles: []
})
export class App {}
