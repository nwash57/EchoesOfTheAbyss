import { Component, inject } from '@angular/core';
import { ConversationPanelComponent } from './components/conversation-panel/conversation-panel';
import { DetailsPanelComponent } from './components/details-panel/details-panel';
import { GameService } from './services/game.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ConversationPanelComponent, DetailsPanelComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected game = inject(GameService);

  onDifficultyChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = parseInt(input.value, 10);
    this.game.setDifficulty(value);
  }
}
