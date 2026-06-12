// Mirrors the backend GenLauncherWeb.Enums.GameType
export enum GameType {
  Generals = 1,
  ZeroHour = 2
}

export function gameDisplayName(game: GameType): string {
  return game === GameType.Generals ? 'Generals' : 'Zero Hour';
}
