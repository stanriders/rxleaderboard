import { Card, CardHeader, CardBody, CardFooter } from "@nextui-org/card";
import { Divider } from "@nextui-org/divider";
import { BeatmapModel, ScoreModel } from "@/api/types";
import { FC } from "react";
import { User } from "./user";
import { Link } from "@nextui-org/link";
import { formatDistance } from 'date-fns';
import { Spacer } from "@nextui-org/spacer";

type Props = { score: ScoreModel, showPlayer: boolean }

export const Score: FC<Props> = ( props ) => {
  const score = props.score;
  const beatmap = score.beatmap as BeatmapModel;

  return (
    <Card >
      {props.showPlayer ? (
      <>
        <CardHeader className="pl-4 pt-2 pb-1">
          {score.user ? (<User user={score.user}/>) : "???"}
        </CardHeader>
        <Divider/>
      </>) : 
      (<></>)}
      
      <CardBody className="p-2">
        <div className="flex gap-2 items-center justify-center">
          <div className="hidden md:block flex-none text-default-400 text-center w-10 text-lg xs:w-8 xs:text-xs">{score.grade}</div>
          <div className="flex flex-col flex-auto min-w-1">
            <div className="truncate"><Link href={`/beatmaps/${score.beatmapId}`} className="text-secondary-600 text-sm" size="sm">{beatmap.artist} - {beatmap.title}<Spacer x={2}/><span className="truncate text-secondary-900 text-xs">{beatmap.difficultyName}</span></Link></div>
            <div className="flex-auto truncate text-default-500 text-xs">{score.totalScore} / {score.combo}x ( {score.count300} / {score.count100} / {score.count50} / {score.countMiss} )</div>
            <div className="truncate text-default-300 text-xs">{formatDistance(new Date(score.date), new Date(), { addSuffix: true })}</div>
          </div>
          
          <div className="hidden md:block flex-none text-default-500 text-sm">{score.mods}</div>
          <div className="hidden md:block flex-none text-default-400 text-center text-sm w-16">{(score.accuracy * 100).toFixed(2) ?? 0}%</div>
          <div className="hidden md:block flex-none text-default-900 items-center text-center w-24 text-lg">{Math.round(score.pp ?? 0)}pp</div>
        </div>
      </CardBody>
      <CardFooter className="md:hidden flex gap-2 items-center justify-center">
          <div className="flex-none text-default-400 text-center w-10 text-lg">{score.grade}</div>
          <div className="flex-auto text-default-500 text-right text-sm">{score.mods}</div>
          <div className="flex-none text-default-400 text-center text-sm">{(score.accuracy * 100).toFixed(2) ?? 0}%</div>
          <div className="flex-none text-default-900 items-center text-center w-20 text-lg">{Math.round(score.pp ?? 0)}pp</div>
      </CardFooter>
    </Card>
  );
};