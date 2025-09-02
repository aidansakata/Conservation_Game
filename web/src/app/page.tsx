import Link from 'next/link';
import UnityCanvas from '@/src/components/UnityCanvas';

export default function Home() {
	return (
		<div className="min-h-screen p-6">
			<nav className="mb-4 flex gap-4">
				<Link href="/how-to">How To</Link>
				<Link href="/about">About</Link>
				<Link href="/hall-of-fame">Hall of Fame</Link>
			</nav>
			<UnityCanvas selectedLevel={1} />
		</div>
	);
}
