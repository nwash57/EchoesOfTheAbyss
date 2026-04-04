import {
  Component,
  inject,
  effect,
  ViewChild,
  ElementRef,
  AfterViewChecked
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GameService } from '../../services/game.service';
import { Messager } from '../../models/message.model';

@Component({
  selector: 'app-conversation-panel',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './conversation-panel.html'
})
export class ConversationPanelComponent implements AfterViewChecked {
  protected game = inject(GameService);
  protected Messager = Messager;
  protected inputText = '';
  private pendingScroll = false;

  @ViewChild('messagesEnd') private messagesEnd!: ElementRef<HTMLDivElement>;

  constructor() {
    effect(() => {
      this.game.messages(); // track signal
      this.game.isThinking();
      this.pendingScroll = true;
    });
  }

  ngAfterViewChecked(): void {
    if (this.pendingScroll) {
      this.pendingScroll = false;
      this.messagesEnd?.nativeElement?.scrollIntoView({ behavior: 'smooth' });
    }
  }

  protected onSubmit(): void {
    if (!this.inputText.trim() || this.game.isThinking()) return;
    this.game.submitMessage(this.inputText);
    this.inputText = '';
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSubmit();
    }
  }
}
