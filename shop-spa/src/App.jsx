import { useEffect, useState } from "react";

const API_BASE = "https://localhost:44368/api";

function App() {
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

    return (
        <div style={{ maxWidth: 900, margin: "0 auto", padding: 16, fontFamily: "system-ui" }}>
            <h1>Shop SPA (React + REST API)</h1>

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

            {loading && <p> Завантаження...</p>}
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

        </div>
    );
}

export default App;
