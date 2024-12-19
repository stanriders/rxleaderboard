"use client";
import { FC } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableColumn,
  TableHeader,
  TableRow,
} from "@nextui-org/table";
import { formatDistance } from "date-fns";
import { Tooltip } from "@nextui-org/react";

import { ScoreModel, UserModel } from "@/api/types";
import { User } from "@/components/user";
import { Mod } from "@/components/mod";

type Props = { scores: ScoreModel[] };

// this stupid thing exists because of the nextui bug that makes tables not work in ssr
export const BeatmapPageTable: FC<Props> = (props) => {
  let index = 0;

  return (
    <>
      <Table isCompact isStriped fullWidth={true}>
        <TableHeader>
          <TableColumn align="start" width={45}>
            {""}
          </TableColumn>
          <TableColumn align="center" width={45}>
            {""}
          </TableColumn>
          <TableColumn align="center" width={75}>
            Score
          </TableColumn>
          <TableColumn>Player</TableColumn>
          <TableColumn align="center" width={75}>
            Combo
          </TableColumn>
          <TableColumn align="center" width={65}>
            300
          </TableColumn>
          <TableColumn align="center" width={65}>
            100
          </TableColumn>
          <TableColumn align="center" width={65}>
            50
          </TableColumn>
          <TableColumn align="center" width={65}>
            Misses
          </TableColumn>
          <TableColumn align="end">Mods</TableColumn>
          <TableColumn align="center" width={85}>
            Accuracy
          </TableColumn>
          <TableColumn align="center" width={100}>
            PP
          </TableColumn>
          <TableColumn align="center" width={140}>
            Date
          </TableColumn>
        </TableHeader>
        <TableBody>
          {props.scores.map((row: ScoreModel) => {
            if (row.isBest) {
              index++;
            }

            return (
              <TableRow
                key={row.id}
                className={
                  row.isBest ? "opacity-100" : "opacity-30 hover:opacity-70"
                }
              >
                <TableCell className="text-default-500 text-center">
                  {row.isBest ? <>#{index}</> : <>-</>}
                </TableCell>
                <TableCell>{row.grade}</TableCell>
                <TableCell>
                  <p className="text-default-500">{row.totalScore}</p>
                </TableCell>
                <TableCell>
                  <User user={row.user as UserModel} />
                </TableCell>
                <TableCell>
                  <p className="text-default-500">{row.combo}x</p>
                </TableCell>
                <TableCell>
                  <p className="text-default-400">{row.count300}</p>
                </TableCell>
                <TableCell>
                  <p className="text-default-400">{row.count100}</p>
                </TableCell>
                <TableCell>
                  <p className="text-default-400">{row.count50}</p>
                </TableCell>
                <TableCell>
                  <p className="text-default-400">{row.countMiss}</p>
                </TableCell>
                <TableCell>
                  <p className="text-default-500">
                    {row.mods?.map((m) => <Mod key={m} mod={m} />)}
                  </p>
                </TableCell>
                <TableCell>
                  <p className="text-default-500">
                    {(row.accuracy * 100)?.toFixed(2) ?? <>-</>}%
                  </p>
                </TableCell>
                <TableCell className="text-primary-400 font-semibold">
                  {row.pp === null ? "-" : row.pp?.toFixed(1)}
                </TableCell>
                <TableCell className="text-default-500 text-xs">
                  <Tooltip
                    showArrow
                    content={new Date(row.date).toLocaleString()}
                  >
                    {formatDistance(new Date(row.date), new Date(), {
                      addSuffix: true,
                    })}
                  </Tooltip>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </>
  );
};
