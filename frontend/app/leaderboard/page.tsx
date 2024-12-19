import type { Metadata } from "next";

import { headers } from "next/headers";

import { LeaderboardTable } from "./table";

import { ApiBase } from "@/api/address";

export const metadata: Metadata = {
  title: "Leaderboard",
};

export default async function LeaderboardPage() {
  const request = await fetch(`${ApiBase}/countries`, {
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Countries fetch failed, ${error}`));

  let response: string[] | undefined;

  if (request) {
    response = await request.json();
  }

  return <LeaderboardTable countries={response} />;
}
