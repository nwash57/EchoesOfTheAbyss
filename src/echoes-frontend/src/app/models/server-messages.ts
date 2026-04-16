import { WorldContext } from './world-context.model';

export interface NarratorCompleteMessage {
  type: 'narrator_complete';
  id: string;
  speech: string;
  thoughts: string[];
}

export interface ExpositionCompleteMessage {
  type: 'exposition_complete';
  id: string;
  speech: string;
  thoughts: string[];
}

export interface WorldUpdateMessage {
  type: 'world_update';
  worldContext: WorldContext;
}

export interface ImaginationStartingMessage {
  type: 'imagination_starting';
  eval: {
    updatePlayer: boolean;
    updateLocation: boolean;
    updateEquipment: boolean;
  };
}

export interface GameOverMessage {
  type: 'game_over';
  summary: string;
}

export interface RequestSetupMessage {
  type: 'request_setup';
}

export interface RestartConfirmedMessage {
  type: 'restart_confirmed';
}

export interface InputRejectedMessage {
  type: 'input_rejected';
  message: string;
}

export interface RuleViolationsMessage {
  type: 'rule_violations';
  violations: { ruleName: string; description: string; severity: string }[];
}

export interface DebugStateMessage {
  type: 'debug_state';
  round: number;
  plotArc: {
    establishedNpcs: string;
    establishedLocations: string;
    establishedItems: string;
    backstory: string;
    plotPoints: string[];
    currentPlotPointIndex: number;
    climax: string;
    currentObjective: string;
  } | null;
  plotAction: string;
  trackerState: {
    consecutiveOnTrack: number;
    driftScore: number;
  };
  evaluation: {
    updateLocation: boolean;
    updatePlayer: boolean;
    updateEquipment: boolean;
    healthDelta: number;
    logEntry: string;
    plotAlignment: string;
  } | null;
}

export type ServerMessage =
  | NarratorCompleteMessage
  | ExpositionCompleteMessage
  | WorldUpdateMessage
  | ImaginationStartingMessage
  | GameOverMessage
  | RequestSetupMessage
  | RestartConfirmedMessage
  | InputRejectedMessage
  | RuleViolationsMessage
  | DebugStateMessage;

export function parseServerMessage(data: string): ServerMessage | null {
  try {
    const msg = JSON.parse(data);
    if (typeof msg?.type !== 'string') return null;
    return msg as ServerMessage;
  } catch {
    return null;
  }
}
