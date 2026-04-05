export interface ActorDemographics {
  firstName: string;
  lastName: string;
  age: number;
  occupation: string;
}

export interface PlayerStats {
  currentHealth: number;
  maxHealth: number;
  baseArmor: number;
  baseStrength: number;
  hasUsedSecondWind: boolean;
}

export interface Player {
  demographics: ActorDemographics;
  stats: PlayerStats;
}

export interface Coordinates {
  x: number;
  y: number;
}

export interface Location {
  title: string;
  coordinates: Coordinates;
  shortDescription: string;
  longDescription: string;
  type?: 'landmark' | 'notable' | 'default';
}

export enum VerbosityLevel {
  ExtremelyConcise = 'ExtremelyConcise',
  Concise = 'Concise',
  Balanced = 'Balanced',
  Verbose = 'Verbose',
  ExtremelyVerbose = 'ExtremelyVerbose'
}

export const VERBOSITY_LABELS: Record<VerbosityLevel, string> = {
  [VerbosityLevel.ExtremelyConcise]: 'Extremely Concise',
  [VerbosityLevel.Concise]: 'Concise',
  [VerbosityLevel.Balanced]: 'Balanced',
  [VerbosityLevel.Verbose]: 'Verbose',
  [VerbosityLevel.ExtremelyVerbose]: 'Extremely Verbose'
};

export const VERBOSITY_STEPS: VerbosityLevel[] = [
  VerbosityLevel.ExtremelyConcise,
  VerbosityLevel.Concise,
  VerbosityLevel.Balanced,
  VerbosityLevel.Verbose,
  VerbosityLevel.ExtremelyVerbose
];

export enum DifficultyLevel {
  ExtremelyEasy = 'ExtremelyEasy',
  Easy = 'Easy',
  Balanced = 'Balanced',
  Hard = 'Hard',
  ExtremelyHard = 'ExtremelyHard'
}

export const DIFFICULTY_LABELS: Record<DifficultyLevel, string> = {
  [DifficultyLevel.ExtremelyEasy]: 'Extremely Easy',
  [DifficultyLevel.Easy]: 'Easy',
  [DifficultyLevel.Balanced]: 'Balanced',
  [DifficultyLevel.Hard]: 'Hard',
  [DifficultyLevel.ExtremelyHard]: 'Extremely Hard'
};

export const DIFFICULTY_STEPS: DifficultyLevel[] = [
  DifficultyLevel.ExtremelyEasy,
  DifficultyLevel.Easy,
  DifficultyLevel.Balanced,
  DifficultyLevel.Hard,
  DifficultyLevel.ExtremelyHard
];

export enum EquipmentSlot {
  Head = 1,
  Torso,
  Legs,
  Feet,
  RightHand,
  LeftHand
}

export interface EquipmentPiece {
  slot: EquipmentSlot;
  name: string;
  armor: number;
  damage: number;
  description: string;
}

export interface Equipment {
  head: EquipmentPiece | null;
  torso: EquipmentPiece | null;
  legs: EquipmentPiece | null;
  feet: EquipmentPiece | null;
  rightHand: EquipmentPiece | null;
  leftHand: EquipmentPiece | null;
}

export interface WorldContext {
  difficulty: DifficultyLevel;
  narrationVerbosity: VerbosityLevel;
  player: Player;
  currentLocation: Location;
  equipment: Equipment;
}
