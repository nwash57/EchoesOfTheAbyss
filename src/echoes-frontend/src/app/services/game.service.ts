import { Injectable, signal } from '@angular/core';
import { Message, Messager } from '../models/message.model';
import { WorldContext, Equipment, Player, Location, DifficultyLevel, VerbosityLevel } from '../models/world-context.model';

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
  private _messages = signal<Message[]>([]);
  private _worldContext = signal<WorldContext>(EMPTY_WORLD);
  private _knownLocations = signal<Location[]>([]);
  private _isThinking = signal<boolean>(false);
  private _isConnected = signal<boolean>(false);
  private _isGameOver = signal<boolean>(false);
  private _isRequestingSetup = signal<boolean>(false);
  private _storySummary = signal<string>('');

  private _isPlayerUpdating = signal<boolean>(false);
  private _isLocationUpdating = signal<boolean>(false);
  private _isEquipmentUpdating = signal<boolean>(false);

  readonly messages = this._messages.asReadonly();
  readonly worldContext = this._worldContext.asReadonly();
  readonly knownLocations = this._knownLocations.asReadonly();
  readonly isThinking = this._isThinking.asReadonly();
  readonly isConnected = this._isConnected.asReadonly();
  readonly isGameOver = this._isGameOver.asReadonly();
  readonly isRequestingSetup = this._isRequestingSetup.asReadonly();
  readonly storySummary = this._storySummary.asReadonly();

  readonly isPlayerUpdating = this._isPlayerUpdating.asReadonly();
  readonly isLocationUpdating = this._isLocationUpdating.asReadonly();
  readonly isEquipmentUpdating = this._isEquipmentUpdating.asReadonly();

  private ws: WebSocket | null = null;

  startGame(): void {
    this.connect();
  }

  private connect(): void {
    this.ws = new WebSocket('ws://localhost:5000/ws');

    this.ws.onopen = () => {
      this._isConnected.set(true);
      this._isThinking.set(true);
    };

    this.ws.onmessage = (event: MessageEvent) => {
      const msg = JSON.parse(event.data as string);
      this.handleServerMessage(msg);
    };

    this.ws.onclose = () => {
      this._isConnected.set(false);
    };

    this.ws.onerror = () => {
      this._isConnected.set(false);
    };
  }

  private handleServerMessage(msg: Record<string, unknown>): void {
    switch (msg['type']) {
      case 'narrator_complete': {
        const message: Message = {
          id: msg['id'] as string,
          messager: Messager.Narrator,
          speech: msg['speech'] as string,
          thoughts: (msg['thoughts'] as string[]) ?? [],
          isExpanded: false
        };
        this._messages.update(msgs => [...msgs, message]);
        this._isThinking.set(false);
        this._isRequestingSetup.set(false);
        break;
      }
      case 'world_update': {
        const context = msg['worldContext'] as WorldContext;
        this._worldContext.set(context);
        this.addLocationIfNew(context.currentLocation);
        this._isPlayerUpdating.set(false);
        this._isLocationUpdating.set(false);
        this._isEquipmentUpdating.set(false);
        break;
      }
      case 'imagination_starting': {
        const evalData = msg['eval'] as { updatePlayer: boolean, updateLocation: boolean, updateEquipment: boolean };
        this._isPlayerUpdating.set(evalData.updatePlayer);
        this._isLocationUpdating.set(evalData.updateLocation);
        this._isEquipmentUpdating.set(evalData.updateEquipment);
        break;
      }
      case 'game_over': {
        this._storySummary.set(msg['summary'] as string);
        this._isGameOver.set(true);
        this._isThinking.set(false);
        break;
      }
      case 'request_setup': {
        this._isRequestingSetup.set(true);
        this._isThinking.set(false);
        break;
      }
      case 'restart_confirmed': {
        this._messages.set([]);
        this._knownLocations.set([]);
        this._isGameOver.set(false);
        this._storySummary.set('');
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
    if (!text.trim() || !this.ws || this.ws.readyState !== WebSocket.OPEN) return;

    const playerMsg: Message = {
      id: Date.now().toString(),
      messager: Messager.Player,
      speech: text.trim(),
      thoughts: [],
      isExpanded: false
    };

    this._messages.update(msgs => [...msgs, playerMsg]);
    this._isThinking.set(true);
    this.ws.send(JSON.stringify({ type: 'player_input', text: text.trim() }));
  }

  setDifficulty(value: DifficultyLevel): void {
    this._worldContext.update(ctx => ({ ...ctx, difficulty: value }));
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) return;
    this.ws.send(JSON.stringify({ type: 'set_difficulty', difficulty: value }));
  }

  setNarrationVerbosity(value: VerbosityLevel): void {
    this._worldContext.update(ctx => ({ ...ctx, narrationVerbosity: value }));
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) return;
    this.ws.send(JSON.stringify({ type: 'set_narration_verbosity', narrationVerbosity: value }));
  }

  restartGame(): void {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) return;
    this.ws.send(JSON.stringify({ type: 'restart_game' }));
  }

  confirmSetup(): void {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) return;
    this.ws.send(JSON.stringify({ type: 'confirm_setup' }));
  }
}
