import { TestBed } from '@angular/core/testing';
import { GameService } from './game.service';
import { WebSocketService } from './websocket.service';
import { ServerMessage } from '../models/server-messages';
import { Messager } from '../models/message.model';
import { DifficultyLevel, VerbosityLevel } from '../models/world-context.model';

class MockWebSocketService {
  private handler: ((msg: ServerMessage) => void) | null = null;
  isConnected = () => true;
  connected = true;
  sentMessages: object[] = [];

  onMessage(handler: (msg: ServerMessage) => void): void {
    this.handler = handler;
  }

  connect(): void {}
  disconnect(): void {}

  send(payload: object): void {
    this.sentMessages.push(payload);
  }

  simulateMessage(msg: ServerMessage): void {
    this.handler?.(msg);
  }
}

describe('GameService', () => {
  let service: GameService;
  let mockWs: MockWebSocketService;

  beforeEach(() => {
    mockWs = new MockWebSocketService();

    TestBed.configureTestingModule({
      providers: [
        GameService,
        { provide: WebSocketService, useValue: mockWs }
      ]
    });

    service = TestBed.inject(GameService);
  });

  describe('handleServerMessage', () => {
    it('should add narrator message on narrator_complete', () => {
      mockWs.simulateMessage({
        type: 'narrator_complete',
        id: 'msg-1',
        speech: 'You enter a dark cave.',
        thoughts: ['The player seems brave.']
      });

      const messages = service.messages();
      expect(messages.length).toBe(1);
      expect(messages[0].messager).toBe(Messager.Narrator);
      expect(messages[0].speech).toBe('You enter a dark cave.');
      expect(messages[0].thoughts).toEqual(['The player seems brave.']);
      expect(service.isThinking()).toBe(false);
    });

    it('should update world context on world_update', () => {
      const worldContext = {
        difficulty: DifficultyLevel.Hard,
        narrationVerbosity: VerbosityLevel.Verbose,
        player: {
          demographics: { firstName: 'Aldric', lastName: 'Thornwood', age: 28, occupation: 'Sellsword' },
          stats: { currentHealth: 75, maxHealth: 100, baseArmor: 10, baseStrength: 15, hasUsedSecondWind: false }
        },
        currentLocation: {
          title: 'Dark Forest',
          coordinates: { x: 1, y: 2 },
          shortDescription: 'A dark forest',
          longDescription: 'Trees block out the light.',
          type: 'notable' as const
        },
        equipment: { head: null, torso: null, legs: null, feet: null, rightHand: null, leftHand: null },
        adventureLog: ['Entered the forest']
      };

      mockWs.simulateMessage({ type: 'world_update', worldContext });

      expect(service.worldContext().player.demographics.firstName).toBe('Aldric');
      expect(service.worldContext().difficulty).toBe(DifficultyLevel.Hard);
      expect(service.isPlayerUpdating()).toBe(false);
      expect(service.isLocationUpdating()).toBe(false);
      expect(service.isEquipmentUpdating()).toBe(false);
    });

    it('should set updating flags on imagination_starting', () => {
      mockWs.simulateMessage({
        type: 'imagination_starting',
        eval: { updatePlayer: true, updateLocation: false, updateEquipment: true }
      });

      expect(service.isPlayerUpdating()).toBe(true);
      expect(service.isLocationUpdating()).toBe(false);
      expect(service.isEquipmentUpdating()).toBe(true);
    });

    it('should set game over state on game_over', () => {
      mockWs.simulateMessage({
        type: 'game_over',
        summary: 'You died bravely.'
      });

      expect(service.isGameOver()).toBe(true);
      expect(service.storySummary()).toBe('You died bravely.');
      expect(service.isThinking()).toBe(false);
    });

    it('should set requesting setup on request_setup', () => {
      mockWs.simulateMessage({ type: 'request_setup' });

      expect(service.isRequestingSetup()).toBe(true);
      expect(service.isThinking()).toBe(false);
    });

    it('should reset state on restart_confirmed', () => {
      // First add some messages
      mockWs.simulateMessage({
        type: 'narrator_complete',
        id: 'msg-1',
        speech: 'Hello',
        thoughts: []
      });
      expect(service.messages().length).toBe(1);

      mockWs.simulateMessage({ type: 'restart_confirmed' });

      expect(service.messages().length).toBe(0);
      expect(service.isGameOver()).toBe(false);
      expect(service.storySummary()).toBe('');
    });
  });

  describe('submitMessage', () => {
    it('should add player message and send to websocket', () => {
      service.submitMessage('attack the goblin');

      const messages = service.messages();
      expect(messages.length).toBe(1);
      expect(messages[0].messager).toBe(Messager.Player);
      expect(messages[0].speech).toBe('attack the goblin');
      expect(service.isThinking()).toBe(true);
      expect(mockWs.sentMessages).toEqual([
        { type: 'player_input', text: 'attack the goblin' }
      ]);
    });

    it('should not submit empty messages', () => {
      service.submitMessage('   ');

      expect(service.messages().length).toBe(0);
      expect(mockWs.sentMessages.length).toBe(0);
    });
  });

  describe('setDifficulty', () => {
    it('should update world context and send to server', () => {
      service.setDifficulty(DifficultyLevel.Hard);

      expect(service.worldContext().difficulty).toBe(DifficultyLevel.Hard);
      expect(mockWs.sentMessages).toEqual([
        { type: 'set_difficulty', difficulty: DifficultyLevel.Hard }
      ]);
    });
  });

  describe('setNarrationVerbosity', () => {
    it('should update world context and send to server', () => {
      service.setNarrationVerbosity(VerbosityLevel.ExtremelyVerbose);

      expect(service.worldContext().narrationVerbosity).toBe(VerbosityLevel.ExtremelyVerbose);
      expect(mockWs.sentMessages).toEqual([
        { type: 'set_narration_verbosity', narrationVerbosity: VerbosityLevel.ExtremelyVerbose }
      ]);
    });
  });

  describe('location deduplication', () => {
    it('should not add duplicate locations', () => {
      const worldContext = {
        difficulty: DifficultyLevel.Balanced,
        narrationVerbosity: VerbosityLevel.Balanced,
        player: {
          demographics: { firstName: '', lastName: '', age: 0, occupation: '' },
          stats: { currentHealth: 100, maxHealth: 100, baseArmor: 0, baseStrength: 0, hasUsedSecondWind: false }
        },
        currentLocation: {
          title: 'Village',
          coordinates: { x: 0, y: 0 },
          shortDescription: 'A village',
          longDescription: 'A quiet village.',
          type: 'default' as const
        },
        equipment: { head: null, torso: null, legs: null, feet: null, rightHand: null, leftHand: null },
        adventureLog: []
      };

      // Send same location twice
      mockWs.simulateMessage({ type: 'world_update', worldContext });
      mockWs.simulateMessage({ type: 'world_update', worldContext });

      expect(service.knownLocations().length).toBe(1);
    });

    it('should add locations with different coordinates', () => {
      const baseContext = {
        difficulty: DifficultyLevel.Balanced,
        narrationVerbosity: VerbosityLevel.Balanced,
        player: {
          demographics: { firstName: '', lastName: '', age: 0, occupation: '' },
          stats: { currentHealth: 100, maxHealth: 100, baseArmor: 0, baseStrength: 0, hasUsedSecondWind: false }
        },
        equipment: { head: null, torso: null, legs: null, feet: null, rightHand: null, leftHand: null },
        adventureLog: []
      };

      mockWs.simulateMessage({
        type: 'world_update',
        worldContext: {
          ...baseContext,
          currentLocation: { title: 'Village', coordinates: { x: 0, y: 0 }, shortDescription: '', longDescription: '', type: 'default' as const }
        }
      });

      mockWs.simulateMessage({
        type: 'world_update',
        worldContext: {
          ...baseContext,
          currentLocation: { title: 'Forest', coordinates: { x: 1, y: 2 }, shortDescription: '', longDescription: '', type: 'notable' as const }
        }
      });

      expect(service.knownLocations().length).toBe(2);
    });
  });
});
