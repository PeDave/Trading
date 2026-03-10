export default function Footer() {
  return (
    <footer className="h-10 bg-gray-900 border-t border-gray-800 flex items-center justify-center px-4 shrink-0">
      <p className="text-xs text-gray-600">
        © {new Date().getFullYear()} LaBot Kripto — All rights reserved. Trading involves risk.
      </p>
    </footer>
  );
}
