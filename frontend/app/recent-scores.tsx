import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { FC } from "react";

type Props = { scores: ScoreModel[] | null | undefined };

export const RecentScoreTable: FC<Props> = (props) => {
  if (!props.scores)
    return <></>;

  return (
    <div className="hidden md:block">
        <h3>Recent scores</h3>
        <Spacer y={2} />
        <div className="flex flex-col gap-2">
            {props.scores.map((row) => (<><Score score={row} showPlayer={true} key={row.id}/></>))}
        </div>
    </div>
  );
}
