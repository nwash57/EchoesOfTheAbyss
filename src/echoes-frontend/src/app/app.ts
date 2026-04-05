import { Component, inject } from '@angular/core';
import { ConversationPanelComponent } from './components/conversation-panel/conversation-panel';
import { DetailsPanelComponent } from './components/details-panel/details-panel';
import { GameService } from './services/game.service';
import {
  DifficultyLevel,
  VerbosityLevel,
  DIFFICULTY_STEPS,
  VERBOSITY_STEPS,
  DIFFICULTY_LABELS,
  VERBOSITY_LABELS
} from './models/world-context.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ConversationPanelComponent, DetailsPanelComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected game = inject(GameService);

  protected readonly DIFFICULTY_STEPS = DIFFICULTY_STEPS;
  protected readonly VERBOSITY_STEPS = VERBOSITY_STEPS;
  protected readonly DIFFICULTY_LABELS = DIFFICULTY_LABELS;
  protected readonly VERBOSITY_LABELS = VERBOSITY_LABELS;

  onDifficultyChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const index = parseInt(input.value, 10);
    const level = DIFFICULTY_STEPS[index];
    if (level) {
      this.game.setDifficulty(level);
    }
  }

  onVerbosityChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const index = parseInt(input.value, 10);
    const level = VERBOSITY_STEPS[index];
    if (level) {
      this.game.setNarrationVerbosity(level);
    }
  }

  getDifficultyIndex(): number {
    return DIFFICULTY_STEPS.indexOf(this.game.worldContext().difficulty);
  }

  getVerbosityIndex(): number {
    return VERBOSITY_STEPS.indexOf(this.game.worldContext().narrationVerbosity);
  }

  confirmSetup(): void {
    this.game.confirmSetup();
  }
}
