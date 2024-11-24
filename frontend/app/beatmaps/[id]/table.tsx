"use client";
import { FC } from "react";
import { ScoreModel, UserModel } from "@/api/types";
import { User } from "@/components/user";
import { Table, TableBody, TableCell, TableColumn, TableHeader, TableRow } from "@nextui-org/table";
import { Mod } from "@/components/mod";
import { formatDistance } from "date-fns";
import { Tooltip } from "@nextui-org/react";

type Props = { scores: ScoreModel[] };

// this stupid thing exists because of the nextui bug that makes tables not work in ssr
export const BeatmapPageTable: FC<Props> = (props) => {
  return (
    <>
      <Table isStriped isCompact fullWidth={true}>
        <TableHeader>
          <TableColumn width={45} align="start">{""}</TableColumn>
          <TableColumn width={45} align="center">{""}</TableColumn>
          <TableColumn width={75} align="center">Score</TableColumn>
          <TableColumn>Player</TableColumn>
          <TableColumn width={75} align="center">Combo</TableColumn>
          <TableColumn width={65} align="center">300</TableColumn>
          <TableColumn width={65} align="center">100</TableColumn>
          <TableColumn width={65} align="center">50</TableColumn>
          <TableColumn width={65} align="center">Misses</TableColumn>
          <TableColumn align="end">Mods</TableColumn>
          <TableColumn width={85} align="center">Accuracy</TableColumn>
          <TableColumn width={100} align="center">PP</TableColumn>
          <TableColumn width={140} align="center">Date</TableColumn>
        </TableHeader>
        <TableBody>
          {props.scores.map((row: ScoreModel, index: number) => (
            <TableRow key={row.id} className={row.isBest ? "opacity-100" : "opacity-30 hover:opacity-70"}>
              <TableCell className="text-default-500">#{index+1}</TableCell>
              <TableCell>{row.grade}</TableCell>
              <TableCell><p className="text-default-500">{row.totalScore}</p></TableCell>
              <TableCell><User user={row.user as UserModel}/></TableCell>
              <TableCell><p className="text-default-500">{row.combo}x</p></TableCell>
              <TableCell><p className="text-default-400">{row.count300}</p></TableCell>
              <TableCell><p className="text-default-400">{row.count100}</p></TableCell>
              <TableCell><p className="text-default-400">{row.count50}</p></TableCell>
              <TableCell><p className="text-default-400">{row.countMiss}</p></TableCell>
              <TableCell><p className="text-default-500">{row.mods?.map(m => <Mod key={m} mod={m}/>)}</p></TableCell>
              <TableCell><p className="text-default-500">{(row.accuracy * 100)?.toFixed(2) ?? (<>-</>)}%</p></TableCell>
              <TableCell className="text-primary-300 font-semibold">{row.pp === null ? "-" : row.pp?.toFixed(1)}</TableCell>
              <TableCell className="text-default-500 text-xs"><Tooltip showArrow content={new Date(row.date).toLocaleString()}>{formatDistance(new Date(row.date), new Date(), { addSuffix: true })}</Tooltip></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </>
  );
};
