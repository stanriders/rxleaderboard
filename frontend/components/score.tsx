import { Card, CardHeader, CardBody, CardFooter } from "@nextui-org/card";
import { FC } from "react";
import { Link } from "@nextui-org/link";
import { formatDistance } from "date-fns";
import { Spacer } from "@nextui-org/spacer";
import { Tooltip } from "@nextui-org/tooltip";
import { CircularProgress } from "@nextui-org/progress";

import { Mod } from "./mod";
import { User } from "./user";

import { BeatmapModel, ScoreModel } from "@/api/types";

type Props = { score: ScoreModel; showPlayer: boolean; simple?: boolean };

export const Score: FC<Props> = (props) => {
  const score = props.score;
  const beatmap = score.beatmap as BeatmapModel;
  const isCalculating = score.beatmap?.status == "Ranked" && score.pp == null;
  const opacity =
    score.isBest || isCalculating
      ? "opacity-100"
      : "opacity-30 hover:opacity-70";

  let pp = (
    <Tooltip content={score.pp?.toFixed(2)} delay={300} showArrow={true}>
      <span>{score.pp?.toFixed(0)} pp</span>
    </Tooltip>
  );

  if (score.pp == null) {
    if (isCalculating) {
      pp = (
        <Tooltip content="Score is being calculated" showArrow={true}>
          <div className="flex justify-center">
            <CircularProgress aria-label="Calculating..." color="primary" />
          </div>
        </Tooltip>
      );
    } else {
      pp = <>- pp</>;
    }
  }

  if (props.simple) {
    return (
      <Card
        key={score.id}
        fullWidth
        className="min-w-72 bg-default-100 dark:bg-default-50"
        shadow="sm"
      >
        {props.showPlayer ? (
          <>
            <CardHeader className="pl-5 pt-2 pb-2">
              {score.user ? <User user={score.user} /> : "???"}
            </CardHeader>
          </>
        ) : (
          <></>
        )}

        <CardBody className={`p-2 ${opacity} bg-content1 rounded-large`}>
          <div className="flex gap-2 items-center justify-center">
            <div className="block flex-none text-center w-10">
              <Link
                isExternal
                className="text-default-500 text-md"
                href={`https://osu.ppy.sh/scores/${score.id}`}
              >
                {score.grade}
              </Link>
            </div>
            <div className="flex flex-col flex-auto min-w-1">
              <div>
                <Link
                  className="text-primary-500 text-xs"
                  href={`/beatmaps/${score.beatmapId}`}
                  size="sm"
                >
                  {beatmap.artist} - {beatmap.title}
                  <Spacer x={2} />
                  <span className="text-primary-300 text-xs pr-3">
                    {beatmap.difficultyName}
                  </span>
                </Link>
              </div>
              <Tooltip
                showArrow
                content={new Date(score.date).toLocaleString()}
              >
                <div className="hidden md:block w-fit truncate text-default-400 text-xs">
                  {formatDistance(new Date(score.date), new Date(), {
                    addSuffix: true,
                  })}
                </div>
              </Tooltip>
            </div>
            <div className="hidden md:block flex-none text-sm">
              {score.mods?.map((m) => <Mod key={m} mod={m} />)}
            </div>
            <div className="hidden md:block flex-none text-default-600 text-center text-sm w-16">
              {(score.accuracy * 100)?.toFixed(2) ?? <>- </>}%
            </div>
            <div className="hidden md:block flex-none text-primary-400 items-center text-center w-24 text-md font-semibold">
              {pp}
            </div>
          </div>
        </CardBody>
        <CardFooter
          className={`md:hidden flex gap-2 items-center justify-center ${opacity}`}
        >
          <div className="flex-none text-center w-10">
            <Tooltip showArrow content={new Date(score.date).toLocaleString()}>
              <div className="w-fit truncate text-default-400 text-xs">
                {formatDistance(new Date(score.date), new Date(), {
                  addSuffix: true,
                })}
              </div>
            </Tooltip>
          </div>
          <div className="flex-auto text-default-500 text-right text-xs">
            {score.mods?.map((m) => <Mod key={m} mod={m} />)}
          </div>
          <div className="flex-none text-default-500 text-center text-xs">
            {(score.accuracy * 100)?.toFixed(2) ?? <>- </>}%
          </div>
          <div className="flex-none text-primary-300 items-center text-center w-20 text-sm font-semibold">
            {pp}
          </div>
        </CardFooter>
      </Card>
    );
  }

  return (
    <Card
      key={score.id}
      fullWidth
      className="min-w-72 bg-default-100 dark:bg-default-50"
      shadow="sm"
    >
      {props.showPlayer ? (
        <>
          <CardHeader className="pl-5 pt-2 pb-2">
            {score.user ? <User user={score.user} /> : "???"}
          </CardHeader>
        </>
      ) : (
        <></>
      )}

      <CardBody className={`p-2 ${opacity} bg-content1 rounded-large`}>
        <div className="flex gap-2 items-center justify-center">
          <div className="hidden md:block flex-none text-center w-10">
            <Link
              isExternal
              className="text-default-500 text-lg"
              href={`https://osu.ppy.sh/scores/${score.id}`}
            >
              {score.grade}
            </Link>
          </div>
          <div className="flex flex-col flex-auto min-w-1">
            <div>
              <Link
                className="text-primary-500 text-sm"
                href={`/beatmaps/${score.beatmapId}`}
                size="sm"
              >
                {beatmap.artist} - {beatmap.title}
                <Spacer x={2} />
                <span className="text-primary-300 text-xs pr-3">
                  {beatmap.difficultyName}
                </span>
              </Link>
            </div>
            <div className="flex-auto truncate text-default-400 text-xs">
              {score.totalScore} · {score.combo}x ( {score.count300} /{" "}
              {score.count100} / {score.count50} / {score.countMiss} )
            </div>
            <Tooltip showArrow content={new Date(score.date).toLocaleString()}>
              <div className="w-fit truncate text-default-400 text-xs">
                {formatDistance(new Date(score.date), new Date(), {
                  addSuffix: true,
                })}
              </div>
            </Tooltip>
          </div>

          <div className="hidden md:block flex-none text-sm">
            {score.mods?.map((m) => <Mod key={m} mod={m} />)}
          </div>
          <div className="hidden md:block flex-none text-default-600 text-center text-sm w-16">
            {(score.accuracy * 100)?.toFixed(2) ?? <>- </>}%
          </div>
          <div className="hidden md:block flex-none text-primary-400 items-center text-center w-24 text-lg font-semibold">
            {pp}
          </div>
        </div>
      </CardBody>
      <CardFooter
        className={`md:hidden flex gap-2 items-center justify-center ${opacity}`}
      >
        <div className="flex-none text-center w-10">
          <Link
            isExternal
            className="text-default-400 text-md"
            href={`https://osu.ppy.sh/scores/${score.id}`}
          >
            {score.grade}
          </Link>
        </div>
        <div className="flex-auto text-default-500 text-right text-sm">
          {score.mods?.map((m) => <Mod key={m} mod={m} />)}
        </div>
        <div className="flex-none text-default-500 text-center text-sm">
          {(score.accuracy * 100)?.toFixed(2) ?? <>- </>}%
        </div>
        <div className="flex-none text-primary-300 items-center text-center w-20 text-lg font-semibold">
          {pp}
        </div>
      </CardFooter>
    </Card>
  );
};
