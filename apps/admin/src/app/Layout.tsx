import { NavLink, Outlet } from "react-router";

import { cn } from "@/lib/utils";

const LINKS = [
  { to: "/recepcao", label: "Recepção" },
  { to: "/pessoas", label: "Pessoas" },
  { to: "/pauta", label: "Pauta" },
  { to: "/aniversariantes", label: "Aniversariantes" },
];

export function Layout() {
  return (
    <div className="min-h-dvh bg-background text-foreground">
      <header className="border-b">
        <div className="flex h-14 items-center gap-6 px-6">
          <NavLink to="/" className="font-semibold">
            CathedrAll
          </NavLink>
          <nav className="flex items-center gap-4 text-sm">
            {LINKS.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                className={({ isActive }) =>
                  cn(
                    "text-muted-foreground hover:text-foreground",
                    isActive && "text-foreground font-medium",
                  )
                }
              >
                {link.label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  );
}
