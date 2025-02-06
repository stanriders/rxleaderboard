import { headers } from "next/headers";
import { Card, CardBody } from "@nextui-org/card";
import { Spacer } from "@nextui-org/react";

import { StatsResponse } from "@/api/types";
import { ApiBase } from "@/api/address";
import { PlaycountChart } from "@/components/playcount-chart";

export default async function Stats() {
  const request = await fetch(`${ApiBase}/stats`, {
    cache: "no-store",
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Recent scores fetch failed, ${error}`));

  let response: StatsResponse | null | undefined;

  if (request) {
    response = await request.json();
  }

  return (
    <section className="flex flex-col items-center justify-center gap-1 py-6 md:py-8 px-4 text-xl">
      <div className="w-full lg:w-5/6">
        <Card>
          <CardBody>
            <p className="text-sm text-default-500">
              {response?.usersTotal ?? 0} users
            </p>
            <p className="text-sm text-default-500">
              {response?.beatmapsTotal ?? 0} beatmap
            </p>
            <Spacer y={6} />
            <p className="text-sm text-default-500">
              {response?.scoresInAMonth ?? 0} scores submitted in the last 30
              days
            </p>
            <div className="h-48">
              <PlaycountChart
                showDate
                playcountsPerMonth={response?.playcountPerDay}
              />
            </div>
            <Spacer y={6} />
            <p className="text-sm text-default-500">
              {response?.scoresTotal ?? 0} scores
            </p>
            <div className="h-48">
              <PlaycountChart
                leftMargin={10}
                playcountsPerMonth={response?.playcountPerMonth}
              />
            </div>
          </CardBody>
        </Card>
      </div>
    </section>
  );
}
