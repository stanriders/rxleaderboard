import type { Metadata } from "next";
import LeaderboardTable from "./table";

export const metadata: Metadata = {
  title: 'Leaderboard',
};

export default function LeaderboardPage() {
  return <LeaderboardTable />;
}
