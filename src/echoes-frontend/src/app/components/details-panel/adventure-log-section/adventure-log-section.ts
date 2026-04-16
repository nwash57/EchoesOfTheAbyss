import { Component, input } from '@angular/core';

@Component({
  selector: 'app-adventure-log-section',
  standalone: true,
  templateUrl: './adventure-log-section.html'
})
export class AdventureLogSectionComponent {
  log = input.required<string[]>();
}
