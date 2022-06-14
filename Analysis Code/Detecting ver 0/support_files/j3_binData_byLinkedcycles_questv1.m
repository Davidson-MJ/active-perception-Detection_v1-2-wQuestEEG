% j3_binDist_bycycle - contrast detection w QUEST
%
% here we will bin trial information into consecutive (2) gait cycles.

% a little tricky, as gait cycle duration varies.

% to better visualuze error, we will concat 2 successive cycles.

% First take: resample time vector of each gait to 200 points, then average across
% gaits.

clear all; close all;

%Mac:
datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:
% datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';


%%%%%% QUEST DETECT version %%%%%%

cd([datadir filesep 'ProcessedData'])
pfols= dir([pwd  filesep '*summary_data.mat']);
nsubs= length(pfols);
%show ppant list:
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

resampSize = 200; % resample the gait cycle (DUAL CYCLE) to this many samps.
%%
for ippant =1:nsubs
    cd([datadir filesep 'ProcessedData'])    %%load data from import job.
    
    load(pfols(ippant).name, 'HeadPos', 'subjID', 'TargState', 'clickState', 'trial_TargetSummary');
    savename = pfols(ippant).name;
    disp(['Preparing j3 linked GCs ' savename]);
    
    %% Gait extraction.
    % Per trial, extract gait samples (trough to trough), normalize along x
    % axis, and store various metrics.
    
    allgpcnt=[];
    allRpcnt=[];
    alldGC=[];
    rtcounter=1;
    for itrial=1:size(HeadPos,2)
        if HeadPos(itrial).isPrac || HeadPos(itrial).isStationary
            continue
        end
        
        %% subj specific trial rejection
        %% subj specific trial rejection
        skip=0;
        rejTrials_questv1; %toggles 'skip' based on bad trial ID
        if skip==1
            continue
        end
        %%
        
        trs = HeadPos(itrial).Y_gait_troughs;
        pks = HeadPos(itrial).Y_gait_peaks;
        trialTime = HeadPos(itrial).times;
        
        % plot gaits overlayed;
        
        % Head position data:
        tmpPos=  squeeze(HeadPos(itrial).Y);
        tmpSway = squeeze(HeadPos(itrial).Z);
        tmpwalkDir = squeeze(HeadPos(itrial).X);
        
        % quick classification:
        if mod(itrial,2)~=0 % odd numbers
            % Then descending on X, (i.e. first trajectory), more positive z values
            % are left side of the body.
            walkDir= 'IN';
        elseif mod(itrial,2)==0 % even numbers
            % ascending (the return trajectory), pos z values are RHS
            walkDir= 'OUT';
        end
        
        
        
        % targ onset and click (RT) data:
        tmpTarg = squeeze(TargState(itrial).state);
        tmpClick = squeeze(clickState(itrial).state);
        
        
        
        % what trial type?
        targsPresented = max(tmpTarg);
        
        %preAllocate for easy storage
        gaitD=[]; %struct
        [gaitHeadY, gaitTarg, gaitTarg_class]= deal(zeros(length(pks)-2, resampSize)); % we will normalize the vector lengths.
        gaitType={};
        
        %summary info for comparison:
        tOns_sumry= trial_TargetSummary(itrial).targOnsets;
        tCor_sumry = trial_TargetSummary(itrial).targdetected;
        tClickOns_smry= trial_TargetSummary(itrial).clickOnsets;
        tRTs_sumry = trial_TargetSummary(itrial).targRTs;
        tFAs_sumry = trial_TargetSummary(itrial).FalseAlarms;
        
        tContrast_sumry  = trial_TargetSummary(itrial).targContrast;        
        tContrastIDX_sumry  = trial_TargetSummary(itrial).targContrastIndx;
        % first store the resampled double gait information (resampled to
        % 200 points)
        
        for igait=1:length(pks)-2
            
            % now using 2 steps!
            gaitsamps =[trs(igait):trs(igait+2)-1]; % trough to frame before returning to ground.
            gaitTimes = trialTime(gaitsamps);
            
            % head Y data this gait:
            gaitDtmp = tmpPos(gaitsamps);
            
            %head sway (z) this gait:
            gaitZtmp = tmpSway(gaitsamps);
            ftis = trial_TargetSummary(itrial).gaitData(igait).peak;
            if strcmp(ftis, 'LR')
                gaitD(igait).peak = 'LRL';
            else %Right ft starts
            
                gaitD(igait).peak = 'RLR';
            end
            % normalize height between 0 and 1
            gaitDtmp_n = rescale(gaitDtmp);
            
            % store key data in matrix for easy handling:
            gaitHeadY(igait,:) = imresize(gaitDtmp_n', [1,resampSize]);
            
            %also store head Y info:
            gaitD(igait).Head_Yraw = gaitDtmp;
            gaitD(igait).Head_Ynorm = gaitDtmp_n;
            gaitD(igait).Head_Y_resampled = imresize(gaitDtmp_n', [1,resampSize]);
            gaitD(igait).gaitsamps = gaitsamps;
            gaitD(igait).gaitTimes = gaitTimes;
            gaitD(igait).gaitTimes_strt_fin = [gaitTimes(1) gaitTimes(end)];
            
        end  % end each gait, resampling position information.
        
        trial_TargetSummary(itrial).gaitHeadY_doubleGC= gaitHeadY;
        trial_TargetSummary(itrial).gaitType = {gaitD(:).peak};
        
        
        %Now, continue by storing targ onsets, and resps, using summary data as
        %ground truth (this avoids adding clicks in the frame x frame which
        %might be false alarms).
        %% determine target onset as % of gait:
        
        for itargO = 1:length(tOns_sumry)
            
            trgIS = tOns_sumry(itargO);
            %step through each gait (the times), and find relevant one containing trg onset.
            for igait = 1:length(gaitD)
                tmptimes= gaitD(igait).gaitTimes;
                % continue if outside range.
                if trgIS< tmptimes(1) || trgIS> tmptimes(end)
                    continue
                end
                
                if trgIS >=tmptimes(1) && trgIS <= tmptimes(end) % event is within this gait.
                    %find gait pcnt.
                    idx = dsearchn(tmptimes, trgIS);
                    
%                     gPcnt = round((idx/length(tmptimes))*resampSize);
                    
                    resizeT= imresize(tmptimes', [1,resampSize]);
                    gPcnt = dsearchn(resizeT', trgIS);
                    
                    %store info using summary and accurate framexframe data:
                    
                    %note that we may have multiple targets / reps per
                    %gait!
                    %if first case, store:
                    if isfield(gaitD(igait), 'tOnset_inTrialTime')
                        
                        %append to existing data:
                        
                        gaitD(igait).tOnset_inTrialTime =  [gaitD(igait).tOnset_inTrialTime, tmptimes(idx)];
                        gaitD(igait).tOnset_inGait = [gaitD(igait).tOnset_inGait ,idx];
                        gaitD(igait).tOnset_inGaitResampled = [gaitD(igait).tOnset_inGaitResampled ,gPcnt];
                        gaitD(igait).targRT_fromsmry = [gaitD(igait).targRT_fromsmry , tRTs_sumry(itargO)];
                        % was the resp correct or no? (Hit or Miss).
                        gaitD(igait).tTargDetected= [gaitD(igait).tTargDetected, tCor_sumry(itargO)];
                         gaitD(igait).tContrast = [gaitD(igait).tContrast, tContrast_sumry(itargO)]; % contrast (value)                    
                        gaitD(igait).tContrastIDX = [gaitD(igait).tContrastIDX ,tContrastIDX_sumry(itargO)]; % contrast (index in range)
                   
                    else % first targ, simply store
                        gaitD(igait).tOnset_inTrialTime =  tmptimes(idx);
                        gaitD(igait).tOnset_inGait = idx;
                        gaitD(igait).tOnset_inGaitResampled = gPcnt;
                        gaitD(igait).targRT_fromsmry = tRTs_sumry(itargO);
                        % was the resp correct or no? (Hit or Miss).
                        gaitD(igait).tTargDetected= tCor_sumry(itargO);
                         gaitD(igait).tContrast = tContrast_sumry(itargO); % contrast (value)                    
                         gaitD(igait).tContrastIDX = tContrastIDX_sumry(itargO); % contrast (index in range)
                   
                    end
                end
                
            end% gait
        end % targOns
        
        %% now do the same for all recorded, responses this trial.
        for irespO = 1:length(tClickOns_smry)
            
            respIS = tClickOns_smry(irespO);
            
            if respIS==0 % i.e. the placeholder for a 'miss' to target.
                continue
            end
            %step through each gait (the times), and find relevant one containing click onset.
            for igait = 1:length(gaitD)
                tmptimes= gaitD(igait).gaitTimes;
                % continue if outside range.
                if respIS < tmptimes(1) || respIS > tmptimes(end)
                    continue
                end
                
                if respIS >=tmptimes(1) && respIS  <= tmptimes(end) % event is within this gait.
                    %find gait pcnt.
                    idx = dsearchn(tmptimes, respIS );
                    
%                     rPcnt = round((idx/length(tmptimes))*resampSize);
                     resizeT= imresize(tmptimes', [1,resampSize]);
                    rPcnt = dsearchn(resizeT', respIS);
                    
                    %store info using summary and accurate framexframe data:
                    if isfield(gaitD(igait), 'response_rawsamp')
                        
                        %append to existing data:
                        
                        gaitD(igait).response_rawsamp =[gaitD(igait).response_rawsamp, idx];
                        gaitD(igait).response_rawtime = [gaitD(igait).response_rawtime, tmptimes(idx)];
                        gaitD(igait).response_rawRT_fromsmry =[gaitD(igait).response_rawRT_fromsmry , tRTs_sumry(irespO)];
                        gaitD(igait).response_resamp = [gaitD(igait).response_resamp rPcnt];
                        gaitD(igait).response_corr= [gaitD(igait).response_corr tCor_sumry(irespO)];
                    else
                        %add for first time.
                        gaitD(igait).response_rawsamp =idx;
                        gaitD(igait).response_rawtime = tmptimes(idx);
                        gaitD(igait).response_rawRT_fromsmry = tRTs_sumry(irespO);
                        gaitD(igait).response_resamp = rPcnt;
                        gaitD(igait).response_corr= tCor_sumry(irespO);
                    end
                    
                end
                
            end% gait
            
        end % irespO
        %% same for any False Alarms:
        
        if any(tFAs_sumry >0)
            for iFAO= 1:length(tFAs_sumry)
                
                respIS = tFAs_sumry(iFAO);
                if respIS==0
                    continue
                end
                %step through each gait (the times), and find relevant one containing click onset.
                for igait = 1:length(gaitD)
                    tmptimes= gaitD(igait).gaitTimes;
                    % continue if outside range.
                    if respIS < tmptimes(1) || respIS > tmptimes(end)
                        continue
                    end
                    
                    if respIS >=tmptimes(1) && respIS  <= tmptimes(end) % event is within this gait.
                        %find gait pcnt.
                        idx = dsearchn(tmptimes, respIS );
                        
%                         rPcnt = round((idx/length(tmptimes))*resampSize);
                       resizeT= imresize(tmptimes', [1,resampSize]);
                    gPcnt = dsearchn(resizeT', respIS);
                        %store info using summary and accurate framexframe data:
                        if isfield(gaitD(igait), 'FA_rawsamp')
                            
                            %append to existing data:
                            gaitD(igait).FA_rawsamp =[gaitD(igait).FA_rawsamp idx];
                            gaitD(igait).FA_rawtime = [gaitD(igait).FA_rawtime tmptimes(idx)];
                            gaitD(igait).FA_rawRT_fromsmry = [gaitD(igait).FA_rawRT_fromsmry tRTs_sumry(irespO)];
                            gaitD(igait).FA_resamp = [gaitD(igait).FA_resamp rPcnt];
                            
                        else
                            %add for first time.
                            gaitD(igait).FA_rawsamp = idx;
                            gaitD(igait).FA_rawtime = tmptimes(idx);
                            gaitD(igait).FA_rawRT_fromsmry = tRTs_sumry(irespO);
                            gaitD(igait).FA_resamp = rPcnt;
                        end
                        
                        
                        
                    end
                end% gait
            end % iFalse alarm
        end
        
        
        % store array of all gait events, at trial level:
        if targsPresented
            trial_TargetSummary(itrial).gaitTargs_detected = [gaitD(:).tTargDetected];
            trial_TargetSummary(itrial).gaitTargs_RT = [gaitD(:).targRT_fromsmry];
            trial_TargetSummary(itrial).gaitTargOns_resamp = [gaitD(:).tOnset_inGaitResampled];
        end
        try
            trial_TargetSummary(itrial).gaitResp_resamp = [gaitD(:).response_resamp];
            trial_TargetSummary(itrial).gaitResp_rawRT = [gaitD(:).response_rawRT_fromsmry];
        catch
        end
        
        
        % save the detailed gait info per trial in structure as well.
        HeadPos(itrial).gaitData_doubGC = gaitD;
        trial_TargetSummary(itrial).gaitData_doubGC = gaitD;
        
        
        
    end %trial
    %% store all relevant info (per double Gait) in a big table:
    % see previous script (j2_binData_bycycle.m) for details:
    % that way we can also perform regressions etc., with greater ease
    % (using col headers).
    % here we specify the column names, and datatypes.
    
    cfg=[];
    cfg.subjID=subjID;
    cfg.nGait = 2;
   Ppant_gaitData = createGaitTableData(trial_TargetSummary, cfg);
   
    
    %% sanity check plot:
%     
%     tmp = Ppant_gaitData.clickPcnt; % compare to the rpcnts collected in plotdebuggait above.
%     rpcnts = tmp(~isnan(tmp));
%     tmpT = Ppant_gaitData.targPosPcnt;
% %     %
% %     clf; 
% %     subplot(211);
%     histogram(rpcnts,resampSize); title('response onset pos');
%    data_binned=[];
%     pidx= ceil(linspace(1,200,14));% length 13
%     for ibin=1:length(pidx)-1
%         idx = pidx(ibin):pidx(ibin+1);
%         
%         data_binned(ibin) = nanmean(tmpT(idx));
%     end
%     subplot(212)
%     bar(data_binned); title('targ onset pos')
    %% add PFX headY
    expindx= [HeadPos(:).isPrac];
    nprac= length(find(expindx>0));
    
    ntrials = size(HeadPos,2) - nprac;
    [PFX_headY]= deal(zeros(ntrials,resampSize));
    
    
    for itrial= 1:size(trial_TargetSummary,2)
        if HeadPos(itrial).isPrac || HeadPos(itrial).isStationary
            continue
        end
        %% subj specific trial rejection
        skip=0;
        rejTrials_questv1; %toggles 'skip' based on bad trial ID
        if skip==1
            continue
        end
        
        
        nGaits = length(trial_TargetSummary(itrial).gaitData_doubGC);
        allgaits = 1:nGaits;
        % omit first and last gaitcycles from each trial?
        usegaits = allgaits(3:end-2);
        
        %data of interest is the resampled gait (1:100) with a position of
        %the targ (now classified as correct or no).
        TrialY= trial_TargetSummary(itrial).gaitHeadY_doubleGC(usegaits,:);
        PFX_headY(itrial,:)= mean(TrialY,1);
        
    end % all trials
    
    %%
    Ppant_gaitData_doubGC= Ppant_gaitData;
    PFX_headY_doubleGC=PFX_headY;
    
    save(savename, 'HeadPos', 'PFX_headY_doubleGC','trial_TargetSummary',...
        'Ppant_gaitData_doubGC','-append');
    
    
    %
end % subject

