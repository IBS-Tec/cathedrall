export interface Random {
  next(): number;
  int(min: number, max: number): number;
  chance(probability: number): boolean;
  pick<T>(items: readonly T[]): T;
  uuid(): string;
}

const HEX = "0123456789abcdef";

export function createRandom(seed: number): Random {
  let state = seed;

  function next(): number {
    state |= 0;
    state = (state + 0x6d2b79f5) | 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }

  function int(min: number, max: number): number {
    return min + Math.floor(next() * (max - min + 1));
  }

  return {
    next,
    int,
    chance(probability: number): boolean {
      return next() < probability;
    },
    pick<T>(items: readonly T[]): T {
      return items[int(0, items.length - 1)];
    },
    uuid(): string {
      let digits = "";
      for (let i = 0; i < 32; i += 1) digits += HEX[int(0, 15)];
      return [
        digits.slice(0, 8),
        digits.slice(8, 12),
        `4${digits.slice(13, 16)}`,
        `a${digits.slice(17, 20)}`,
        digits.slice(20),
      ].join("-");
    },
  };
}
