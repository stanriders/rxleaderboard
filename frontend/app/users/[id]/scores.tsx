"use client";
import { Spacer } from "@nextui-org/spacer";
import { FC, useState } from "react";
import { Switch } from "@nextui-org/switch";

import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";

type Props = { scores: ScoreModel[] | null | undefined };

export const ScoresContainer: FC<Props> = (props) => {
  const [showAll, setShowAll] = useState(false);

  if (!props.scores) return <></>;

  const scores = props.scores ?? [];
  const shouldShowSwitch = props.scores.find((r) => !r.isBest) != null;

  return (
    <>
      {shouldShowSwitch ? (
        <div className="flex justify-end pb-4">
          <Switch isSelected={showAll} size="sm" onValueChange={setShowAll}>
            Show all scores
          </Switch>
        </div>
      ) : (
        <></>
      )}

      {scores
        .filter((row: ScoreModel) => {
          return !(!showAll && !row.isBest);
        })
        .map((row: ScoreModel, index) => {
          return (
            <>
              <Score
                key={row.id}
                rank={index + 1}
                score={row}
                showPlayer={false}
              />
              <Spacer y={1} />
            </>
          );
        })}
    </>
  );
};
