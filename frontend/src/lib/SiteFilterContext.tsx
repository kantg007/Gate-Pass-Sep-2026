import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, type Site } from "../lib/api";

type SiteFilterState = {
  sites: Site[];
  siteId: string;
  setSiteId: (id: string) => void;
  loading: boolean;
};

const SiteFilterContext = createContext<SiteFilterState | null>(null);

export function SiteFilterProvider({ children }: { children: ReactNode }) {
  const [sites, setSites] = useState<Site[]>([]);
  const [siteId, setSiteId] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void (async () => {
      try {
        setSites(await api.listSites());
      } catch {
        setSites([]);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const value = useMemo(
    () => ({ sites, siteId, setSiteId, loading }),
    [sites, siteId, loading],
  );

  return <SiteFilterContext.Provider value={value}>{children}</SiteFilterContext.Provider>;
}

export function useSiteFilter() {
  const ctx = useContext(SiteFilterContext);
  if (!ctx) throw new Error("useSiteFilter outside provider");
  return ctx;
}
