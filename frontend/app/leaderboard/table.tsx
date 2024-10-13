"use client";
import { Table, TableHeader, TableColumn, TableBody, TableRow, TableCell } from "@nextui-org/table";
import { Pagination } from "@nextui-org/pagination";
import { Skeleton } from "@nextui-org/skeleton";
import { UserModel } from "@/api/types";
import { useState } from 'react'
import useSWR from 'swr'
import { User } from "@/components/user";
import { ApiBase } from "@/api/address";

const fetcher = (url : any) => fetch(url).then(r => r.json()).catch(error => console.log("Leaderboard fetch error: " + error));;

export default function LeaderboardTable() {
    const [page, setPage] = useState(1);

    const { data, error } = useSWR(`${ApiBase}/players?page=${page}`, fetcher)

    const placeholderRows = Array.from(Array(50), (_, i) => (<TableRow key={i}>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
    </TableRow>))

    if (error) return <div>Failed to load</div>
    if (!data) 
    return (<>
        <Table isCompact fullWidth={true} bottomContent={
          <div className="flex w-full justify-center"><Pagination isCompact showControls showShadow color="secondary" total={0} /></div>
          }>
        <TableHeader>
          <TableColumn width={55} align="start">{""}</TableColumn>
          <TableColumn className="text-ellipsis">Username</TableColumn>
          <TableColumn align="center">Accuracy</TableColumn>
          <TableColumn align="center">PP</TableColumn>
        </TableHeader>
        <TableBody>{placeholderRows}</TableBody>
      </Table>
        </>)

    const pages = Math.ceil(data.total / 50);
    const offset = (page-1) * 50;
    let players = data.players;

    return (
    <>    
      <Table isStriped isCompact fullWidth={true}
          bottomContent={
          <div className="flex w-full justify-center">
            <Pagination
              isCompact
              showControls
              showShadow
              color="primary"
              page={page}
              total={pages}
              onChange={(page) => {setPage(page)}}
            />
          </div>
        }>
        <TableHeader>
          <TableColumn width={55} align="start">{""}</TableColumn>
          <TableColumn >Username</TableColumn>
          <TableColumn align="center">Accuracy</TableColumn>
          <TableColumn align="center">PP</TableColumn>
        </TableHeader>
        <TableBody>
          {players.map((row: UserModel, index: number) => (
            <TableRow key={row.id}>
              <TableCell className="text-default-500">#{offset+index+1}</TableCell>
              <TableCell><User user={row}/></TableCell>
              <TableCell><p className="text-default-500">{row.totalAccuracy === null ? "-" : row.totalAccuracy?.toFixed(2)}%</p></TableCell>
              <TableCell className="text-primary-300 font-semibold">{row.totalPp === null ? "-" : row.totalPp?.toFixed(0)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </>
  );
}
