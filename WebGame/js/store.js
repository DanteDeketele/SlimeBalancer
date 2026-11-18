// PlayerPrefs replacement. localStorage can throw (private mode, blocked storage),
// so every access is guarded and falls back to memory.
const memory = new Map();
const PREFIX = "slimebalancer.";

export const store = {
  get(key, fallback) {
    try {
      const raw = localStorage.getItem(PREFIX + key);
      if (raw !== null) return JSON.parse(raw);
    } catch {
      if (memory.has(key)) return memory.get(key);
    }
    return memory.has(key) ? memory.get(key) : fallback;
  },
  set(key, value) {
    memory.set(key, value);
    try {
      localStorage.setItem(PREFIX + key, JSON.stringify(value));
    } catch {
      /* memory only */
    }
  },
  remove(key) {
    memory.delete(key);
    try {
      localStorage.removeItem(PREFIX + key);
    } catch {
      /* ignore */
    }
  },
};
