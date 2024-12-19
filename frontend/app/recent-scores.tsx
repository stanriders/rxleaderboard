import { Spacer } from "@nextui-org/spacer";
import { FC } from "react";

import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";

type Props = { scores: ScoreModel[] | null | undefined };

export const RecentScoreTable: FC<Props> = (props) => {
  if (!props.scores) return <></>;

  return (
    <div className="hidden md:block w-full lg:w-5/6">
      <h3>Recent scores</h3>
      <Spacer y={2} />
      <div className="flex flex-col gap-2">
        {props.scores.map((row) => (
          <>
            <Score key={row.id} score={row} showPlayer={true} />
          </>
        ))}
      </div>
    </div>
  );
};
