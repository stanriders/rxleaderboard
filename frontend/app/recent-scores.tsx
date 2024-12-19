import { Spacer } from "@nextui-org/spacer";
import { FC } from "react";

import { Score } from "@/components/score";
import { RecentScoresResponse } from "@/api/types";

type Props = { response: RecentScoresResponse | null | undefined };

export const RecentScoreTable: FC<Props> = (props) => {
  if (!props.response) return <></>;

  const scores = props.response.scores ?? [];

  return (
    <div className="hidden md:block w-full lg:w-5/6">
      <h3>Recent scores</h3>
      <p className="text-xs text-default-400">
        ({props.response.scoresToday ?? 0} submitted in the last 24hrs)
      </p>
      <Spacer y={2} />
      <div className="flex flex-col gap-2">
        {scores.length > 0 ? (
          scores.map((row) => (
            <>
              <Score key={row.id} score={row} showPlayer={true} />
            </>
          ))
        ) : (
          <>No scores!</>
        )}
      </div>
    </div>
  );
};
