import { useEffect, useMemo, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import MarkerClusterGroup from "react-leaflet-cluster";

import "leaflet/dist/leaflet.css";
import "leaflet.markercluster/dist/MarkerCluster.css";
import "leaflet.markercluster/dist/MarkerCluster.Default.css";

const API_BASE = "https://localhost:44368/api";

function FitBounds({ points }) {
  const map = useMap();
  const bounds = useMemo(() => {
    if (!points?.length) return null;
    const lats = points.map((p) => p.lat);
    const lngs = points.map((p) => p.lng);
    const min = [Math.min(...lats), Math.min(...lngs)];
    const max = [Math.max(...lats), Math.max(...lngs)];
    return [min, max];
  }, [points]);

  useEffect(() => {
    if (bounds) map.fitBounds(bounds, { padding: [30, 30] });
  }, [bounds, map]);

  return null;
}

export default function MapPage() {
  const [q, setQ] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [countryId, setCountryId] = useState("");
  const [points, setPoints] = useState([]);
  const [loading, setLoading] = useState(false);

  async function load() {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (q) params.set("q", q);
      if (categoryId) params.set("categoryId", categoryId);
      if (countryId) params.set("countryId", countryId);
      params.set("limit", "1000");

      const res = await fetch(`${API_BASE}/items/map?${params.toString()}`);
      const data = await res.json();
      setPoints(data.data ?? []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div style={{ display: "grid", gridTemplateRows: "auto 1fr", height: "100vh" }}>
      <div style={{ display: "flex", gap: 8, padding: 8, alignItems: "center", borderBottom: "1px solid #eee" }}>
        <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Пошук (Lucene)..." style={{ padding: 6, flex: 1 }} />
        <input value={categoryId} onChange={(e) => setCategoryId(e.target.value)} placeholder="CategoryId" style={{ width: 120, padding: 6 }} />
        <input value={countryId} onChange={(e) => setCountryId(e.target.value)} placeholder="CountryId" style={{ width: 120, padding: 6 }} />
        <button onClick={load} disabled={loading}>{loading ? "Завантаження…" : "Фільтрувати"}</button>
        <div style={{ opacity: 0.7 }}>Точок: {points.length}</div>
      </div>

      <MapContainer center={[48.3794, 31.1656]} zoom={5} style={{ width: "100%", height: "100%" }}>
        <TileLayer
          attribution="&copy; OpenStreetMap contributors"
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <FitBounds points={points} />

        {/* КЛАСТЕРИ*/}
        <MarkerClusterGroup
          chunkedLoading
          maxClusterRadius={60}
          spiderfyOnEveryZoom={false}
          showCoverageOnHover={false}
        >
          {points.map((p) => (
            <Marker key={p.id} position={[p.lat, p.lng]}>
              <Popup>
                <b>{p.name}</b><br />
                Категорія: {p.categoryName ?? p.categoryId}<br />
                Країна: {p.countryName ?? p.countryId}<br />
                Ціна: {p.price} грн
              </Popup>
            </Marker>
          ))}
        </MarkerClusterGroup>
      </MapContainer>
    </div>
  );
}
