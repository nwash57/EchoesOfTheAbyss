import { Component, input, model } from '@angular/core';

@Component({
  selector: 'app-accordion-section',
  standalone: true,
  templateUrl: './accordion-section.html'
})
export class AccordionSectionComponent {
  title = input.required<string>();
  expanded = model(true);
  badge = input<string | null>(null);

  toggle(): void {
    this.expanded.set(!this.expanded());
  }
}
