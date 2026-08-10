import { Outlet } from "react-router";

export function Layout() {
  return (
    <div className="min-h-dvh bg-background text-foreground">
      <header className="border-b">
        <div className="flex h-14 items-center px-6">
          <span className="font-semibold">CathedrAll</span>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  );
}
