import { useEffect, useState } from "react";
import MapPage from "./MapPage";

const API_BASE = "https://localhost:44368/api";

export default function App() {
  const [view, setView] = useState("list"); // 'list' | 'map'
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [skip, setSkip] = useState(0);
  const [limit, setLimit] = useState(10);
  const [nextLink, setNextLink] = useState(null);

  async function loadItems(newSkip = 0, q = "") {
    setLoading(true);
    setError("");
    try {
      const url = q
        ? `${API_BASE}/items/search?q=${encodeURIComponent(q)}&skip=${newSkip}&limit=${limit}`
        : `${API_BASE}/items?skip=${newSkip}&limit=${limit}`;

      const res = await fetch(url);
      if (!res.ok) throw new Error("Помилка отримання даних");
      const data = await res.json();

      setItems(data.data ?? []);
      setSkip(data.skip ?? 0);
      setLimit(data.limit ?? 10);
      setNextLink(data.nextLink ?? null);
    } catch (e) {
      console.error(e);
      setError("Не вдалося завантажити товари");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadItems(0, "");
  }, []);

  if (view === "map") {
    return (
      <div style={{ height: "100vh" }}>
        <div style={{ padding: 8, borderBottom: "1px solid #eee" }}>
          <button onClick={() => setView("list")}>← Повернутися до списку</button>
        </div>
        <MapPage />
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 900, margin: "0 auto", padding: 16, fontFamily: "system-ui" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
        <h1>Shop SPA (React + REST API)</h1>
        <button onClick={() => setView("map")}>🗺 Перейти на мапу</button>
      </div>

      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Пошук товарів…"
          style={{ flex: 1, padding: 6 }}
        />
        <button onClick={() => loadItems(0, search)}>Шукати</button>
        <button onClick={() => { setSearch(""); loadItems(); }}>Скинути</button>
      </div>

      {loading && <p>Завантаження...</p>}
      {error && <p style={{ color: "red" }}>{error}</p>}

      <ul style={{ listStyle: "none", padding: 0 }}>
        {items.map(it => (
          <li key={it.id} style={{ border: "1px solid #ccc", marginBottom: 8, padding: 8, borderRadius: 6 }}>
            <b>{it.name}</b> — {it.price} грн
            <div style={{ fontSize: 13, color: "#666" }}>
              Категорія: {it.categoryName ?? it.categoryId} | Країна: {it.countryName ?? it.countryId}
            </div>
          </li>
        ))}
      </ul>

      <div style={{ marginTop: 12, display: "flex", gap: 8 }}>
        <button disabled={skip === 0} onClick={() => loadItems(Math.max(skip - limit, 0), search)}>← Назад</button>
        <button disabled={!nextLink} onClick={() => loadItems(skip + limit, search)}>Далі →</button>
      </div>
    </div>
  );
}
