import { Injectable, inject, signal, computed } from '@angular/core';
import { Message, Messager } from '../models/message.model';
import { WorldContext, Equipment, Player, Location, DifficultyLevel, VerbosityLevel } from '../models/world-context.model';
import { ServerMessage, DebugStateMessage } from '../models/server-messages';
import { WebSocketService } from './websocket.service';

const EMPTY_WORLD: WorldContext = {
  difficulty: DifficultyLevel.Balanced,
  narrationVerbosity: VerbosityLevel.Balanced,
  player: {
    demographics: { firstName: '', lastName: '', age: 0, occupation: '' },
    stats: { currentHealth: 100, maxHealth: 100, baseArmor: 0, baseStrength: 0, hasUsedSecondWind: false }
  },
  currentLocation: {
    title: '',
    coordinates: { x: 0, y: 0 },
    shortDescription: '',
    longDescription: '',
    type: 'default'
  },
  equipment: {
    head: null,
    torso: null,
    legs: null,
    feet: null,
    rightHand: null,
    leftHand: null
  },
  adventureLog: []
};

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly ws = inject(WebSocketService);

  private _messages = signal<Message[]>([]);
  private _worldContext = signal<WorldContext>(EMPTY_WORLD);
  private _knownLocations = signal<Location[]>([]);
  private _isThinking = signal<boolean>(false);
  private _isGameOver = signal<boolean>(false);
  private _isRequestingSetup = signal<boolean>(false);
  private _storySummary = signal<string>('');

  private _isPlayerUpdating = signal<boolean>(false);
  private _isLocationUpdating = signal<boolean>(false);
  private _isEquipmentUpdating = signal<boolean>(false);

  private _debugState = signal<DebugStateMessage | null>(null);

  private _expositionText = signal<string | null>(null);
  private _expositionId = signal<string>('');
  private _expositionThoughts = signal<string[]>([]);

  readonly messages = this._messages.asReadonly();
  readonly worldContext = this._worldContext.asReadonly();
  readonly knownLocations = this._knownLocations.asReadonly();
  readonly isThinking = this._isThinking.asReadonly();
  readonly isConnected = this.ws.isConnected;
  readonly isGameOver = this._isGameOver.asReadonly();
  readonly isRequestingSetup = this._isRequestingSetup.asReadonly();
  readonly storySummary = this._storySummary.asReadonly();

  readonly isPlayerUpdating = this._isPlayerUpdating.asReadonly();
  readonly isLocationUpdating = this._isLocationUpdating.asReadonly();
  readonly isEquipmentUpdating = this._isEquipmentUpdating.asReadonly();

  readonly debugState = this._debugState.asReadonly();

  readonly expositionText = this._expositionText.asReadonly();
  readonly isShowingPrologue = computed(() => this._expositionText() !== null);

  constructor() {
    this.ws.onMessage((msg) => this.handleServerMessage(msg));
  }

  startGame(): void {
    this.ws.connect();
    this._isThinking.set(true);
  }

  handleServerMessage(msg: ServerMessage): void {
    switch (msg.type) {
      case 'exposition_complete': {
        this._expositionText.set(msg.speech);
        this._expositionId.set(msg.id);
        this._expositionThoughts.set(msg.thoughts ?? []);
        this._isThinking.set(false);
        break;
      }
      case 'narrator_complete': {
        const message: Message = {
          id: msg.id,
          messager: Messager.Narrator,
          speech: msg.speech,
          thoughts: msg.thoughts ?? [],
          isExpanded: false
        };
        this._messages.update(msgs => [...msgs, message]);
        this._isThinking.set(false);
        break;
      }
      case 'world_update': {
        this._worldContext.set(msg.worldContext);
        this.addLocationIfNew(msg.worldContext.currentLocation);
        this._isPlayerUpdating.set(false);
        this._isLocationUpdating.set(false);
        this._isEquipmentUpdating.set(false);
        break;
      }
      case 'imagination_starting': {
        this._isPlayerUpdating.set(true);
        this._isLocationUpdating.set(true);
        this._isEquipmentUpdating.set(true);
        break;
      }
      case 'game_over': {
        this._storySummary.set(msg.summary);
        this._isGameOver.set(true);
        this._isThinking.set(false);
        break;
      }
      case 'request_setup': {
        this._messages.set([]);
        this._knownLocations.set([]);
        this._worldContext.set(EMPTY_WORLD);
        this._isGameOver.set(false);
        this._storySummary.set('');
        this._expositionText.set(null);
        this._debugState.set(null);
        this._isRequestingSetup.set(true);
        this._isThinking.set(false);
        break;
      }
      case 'restart_confirmed': {
        this._messages.set([]);
        this._knownLocations.set([]);
        this._isGameOver.set(false);
        this._storySummary.set('');
        this._expositionText.set(null);
        this._debugState.set(null);
        break;
      }
      case 'input_rejected': {
        const rejectionMsg: Message = {
          id: Date.now().toString(),
          messager: Messager.System,
          speech: msg.message ?? 'That action is not allowed.',
          thoughts: [],
          isExpanded: false
        };
        this._messages.update(msgs => [...msgs, rejectionMsg]);
        this._isThinking.set(false);
        break;
      }
      case 'rule_violations': {
        // State corrections are silent — logged for debugging but not shown to player
        console.log('[Rules Engine]', msg.violations);
        break;
      }
      case 'debug_state': {
        this._debugState.set(msg);
        break;
      }
    }
  }

  private addLocationIfNew(loc: Location): void {
    const exists = this._knownLocations().some(
      l => l.coordinates.x === loc.coordinates.x && l.coordinates.y === loc.coordinates.y
    );
    if (!exists) {
      this._knownLocations.update(locs => [...locs, loc]);
    }
  }

  toggleMessageExpanded(id: string): void {
    this._messages.update(msgs =>
      msgs.map(m => (m.id === id ? { ...m, isExpanded: !m.isExpanded } : m))
    );
  }

  submitMessage(text: string): void {
    if (!text.trim() || !this.ws.connected) return;

    const playerMsg: Message = {
      id: Date.now().toString(),
      messager: Messager.Player,
      speech: text.trim(),
      thoughts: [],
      isExpanded: false
    };

    this._messages.update(msgs => [...msgs, playerMsg]);
    this._isThinking.set(true);
    this.ws.send({ type: 'player_input', text: text.trim() });
  }

  setDifficulty(value: DifficultyLevel): void {
    this._worldContext.update(ctx => ({ ...ctx, difficulty: value }));
    this.ws.send({ type: 'set_difficulty', difficulty: value });
  }

  setNarrationVerbosity(value: VerbosityLevel): void {
    this._worldContext.update(ctx => ({ ...ctx, narrationVerbosity: value }));
    this.ws.send({ type: 'set_narration_verbosity', narrationVerbosity: value });
  }

  restartGame(): void {
    this.ws.send({ type: 'restart_game' });
  }

  dismissPrologue(): void {
    const text = this._expositionText();
    if (!text) return;

    const message: Message = {
      id: this._expositionId(),
      messager: Messager.Narrator,
      speech: text,
      thoughts: this._expositionThoughts(),
      isExpanded: false
    };
    this._messages.update(msgs => [...msgs, message]);
    this._expositionText.set(null);
  }

  confirmSetup(): void {
    const ctx = this._worldContext();
    this.ws.send({
      type: 'confirm_setup',
      difficulty: ctx.difficulty,
      narrationVerbosity: ctx.narrationVerbosity
    });
    this._isRequestingSetup.set(false);
    this._isThinking.set(true);
  }
}
